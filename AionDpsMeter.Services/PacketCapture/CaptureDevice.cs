using SharpPcap;
using PacketDotNet;
using System.Collections.Concurrent;
using System.Buffers;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.Services.PacketCapture
{
    /// <summary>
    /// Captures game traffic across all network adapters with loopback preference.
    ///
    /// Detection flow:
    ///   1. All eligible adapters open simultaneously in detection mode.
    ///   2. Heartbeat hits are tallied per adapter+port.
    ///   3. When any adapter+port crosses DetectionThreshold a 2-second grace
    ///      window starts so that VPN loopback traffic has time to appear.
    ///   4. After the window, priority rules pick the winning adapter:
    ///      loopback > physical. The winner's device is locked; all others close.
    ///   5. A watchdog resets everything back to detection if no heartbeat is
    ///      seen on the locked adapter for WatchdogTimeoutMs.
    /// </summary>
    public sealed class CaptureDevice : IPacketCaptureDevice
    {
        private const int KernelBufferSize = 64 * 1024 * 1024;
        private const int ReadTimeoutMs = 100;
        private const int DetectionThreshold = 5;
        private const int GraceWindowMs = 2000;
        private const int WatchdogTimeoutMs = 60_000;

        private static readonly byte[] HeartbeatPattern = { 0x0E, 0x00, 0x36 };
        // ────────────────────────────────────────────────────────────────────────

        private readonly List<AdapterContext> allAdapters = new();

        /// <summary>
        /// Hit counts: adapterName → (port → hitCount).
        /// Written from multiple capture callbacks; guarded by detectionLock.
        /// </summary>
        private readonly Dictionary<string, Dictionary<int, int>> detectionHits = new();

        private readonly object detectionLock = new();

        private volatile bool graceWindowActive;
        private Timer? graceWindowTimer;

        private volatile bool isLocked;       // true once grace window resolved
        private AdapterContext? lockedAdapter;
        private volatile int lockedPort;

        private readonly BlockingCollection<PooledRawPacket> packetQueue;
        private Thread? processingThread;
        private readonly CancellationTokenSource cts = new();

        private Timer? watchdogTimer;
        private volatile int watchdogHeartbeatsSinceCheck;   // reset on each heartbeat hit

        private volatile bool isCapturing;
        private volatile int appDroppedPackets;
        private Timer? statsTimer;

        private readonly Dictionary<string, uint> expectedSeqNumbers = new();
        private readonly ILogger<CaptureDevice> logger;
        private readonly TcpStreamBuffer tcpStreamBuffer;

        public bool IsCapturing => isCapturing;
        public string? DeviceName => lockedAdapter?.Device.Name;

      
        public CaptureDevice(
            ILogger<CaptureDevice> logger,
            TcpStreamBuffer tcpStreamBuffer)
        {
            this.logger = logger;
            this.tcpStreamBuffer = tcpStreamBuffer;
            packetQueue = new BlockingCollection<PooledRawPacket>(100_000);
            DiscoverAdapters();
        }


        private void DiscoverAdapters()
        {
            foreach (var dev in CaptureDeviceList.Instance)
            {
                var kind = ClassifyAdapter(dev);
                if (kind == AdapterKind.Skip) continue;

                allAdapters.Add(new AdapterContext(dev, kind));
                logger.LogDebug($"[DISCOVER] {kind,-8} adapter: {dev.Name} — {dev.Description}");
            }

            if (allAdapters.Count == 0)
                logger.LogWarning("[DISCOVER] No eligible adapters found.");
        }

        private static AdapterKind ClassifyAdapter(ICaptureDevice dev)
        {
            var name = dev.Name ?? string.Empty;
            var desc = dev.Description ?? string.Empty;

            if (desc.Contains("Loopback", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Loopback", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("lo", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("\\Device\\NPF_Loopback", StringComparison.OrdinalIgnoreCase))
                return AdapterKind.Loopback;

            if (desc.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase) ||
                desc.Contains("VMware", StringComparison.OrdinalIgnoreCase) ||
                desc.Contains("VirtualBox", StringComparison.OrdinalIgnoreCase) ||
                desc.Contains("TAP-Windows", StringComparison.OrdinalIgnoreCase) ||
                desc.Contains("WAN Miniport", StringComparison.OrdinalIgnoreCase))
                return AdapterKind.Skip;

            return AdapterKind.Physical;
        }


        public void StartCapture()
        {
            if (isCapturing) return;
            if (allAdapters.Count == 0)
                throw new InvalidOperationException("No eligible network adapters found.");

            ResetAllState();

            processingThread = new Thread(ProcessingLoop)
            {
                Name = "PacketProcessor",
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal
            };
            processingThread.Start();

            foreach (var ctx in allAdapters)
                OpenAdapterForDetection(ctx);

            statsTimer = new Timer(LogDriverStats, null, 5_000, 5_000);
            isCapturing = true;

            logger.LogInformation(
                $"[CAPTURE] Started on {allAdapters.Count} adapter(s). " +
                $"Listening for heartbeat pattern on all. Loopback preferred.");
        }

        public void StopCapture()
        {
            if (!isCapturing) return;
            isCapturing = false;

            statsTimer?.Dispose(); statsTimer = null;
            watchdogTimer?.Dispose(); watchdogTimer = null;
            graceWindowTimer?.Dispose(); graceWindowTimer = null;

            CloseAllAdapters();

            cts.Cancel();
            processingThread?.Join(1_000);

            logger.LogInformation("[CAPTURE] Stopped.");
        }

        public void Dispose()
        {
            StopCapture();
            cts.Dispose();
            packetQueue.Dispose();
        }


        private void OpenAdapterForDetection(AdapterContext ctx)
        {
            try
            {
                ctx.Device.OnPacketArrival += (s, e) => OnPacketArrival(ctx, e);
                ctx.Device.Open(new DeviceConfiguration
                {
                    Mode = DeviceModes.Promiscuous,
                    ReadTimeout = ReadTimeoutMs,
                    BufferSize = KernelBufferSize
                });
                ctx.Device.Filter = "tcp";
                ctx.Device.StartCapture();
                ctx.IsOpen = true;

                logger.LogDebug($"[DETECT] Opened {ctx.Kind} adapter: {ctx.Device.Name}");
            }
            catch (Exception ex)
            {
                logger.LogWarning($"[DETECT] Could not open {ctx.Device.Name}: {ex.Message}");
            }
        }

        private void CloseAdapter(AdapterContext ctx)
        {
            if (!ctx.IsOpen) return;
            try { ctx.Device.StopCapture(); } catch { /* ignored */ }
            try { ctx.Device.Close(); } catch { /* ignored */ }
            ctx.IsOpen = false;
        }

        private void CloseAllAdapters()
        {
            foreach (var ctx in allAdapters)
                CloseAdapter(ctx);
        }

        private void CloseAllExcept(AdapterContext winner)
        {
            foreach (var ctx in allAdapters)
            {
                if (ctx != winner)
                    CloseAdapter(ctx);
            }
        }


        private void OnPacketArrival(AdapterContext ctx, SharpPcap.PacketCapture e)
        {
            try
            {
                var raw = e.GetPacket();
                var length = raw.Data.Length;
                if (length == 0) return;

                byte[] buffer = ArrayPool<byte>.Shared.Rent(length);
                Array.Copy(raw.Data, 0, buffer, 0, length);

                var pooled = new PooledRawPacket(
                    raw.LinkLayerType, buffer, length,
                    raw.Timeval.Date,
                    ctx);  

                if (!packetQueue.TryAdd(pooled))
                {
                    pooled.Dispose();
                    Interlocked.Increment(ref appDroppedPackets);
                }
            }
            catch { /* ignore */ }
        }


        private void ProcessingLoop()
        {
            try
            {
                foreach (var raw in packetQueue.GetConsumingEnumerable(cts.Token))
                {
                    try { ProcessPacket(raw); }
                    catch (Exception ex)
                    { logger.LogError($"Error processing packet: {ex.Message}"); }
                    finally { raw.Dispose(); }
                }
            }
            catch (OperationCanceledException) { }
        }

        private void ProcessPacket(PooledRawPacket raw)
        {
            var packet = Packet.ParsePacket(raw.LinkLayerType, raw.Buffer);
            var tcp = packet.Extract<TcpPacket>();
            if (tcp == null || !tcp.HasPayloadData) return;

            if (!isLocked)
            {
                HandleDetectionPacket(raw.SourceAdapter!, tcp);
                return;
            }

            if (raw.SourceAdapter != lockedAdapter) return;
            if (tcp.SourcePort != lockedPort) return;

            Interlocked.Increment(ref watchdogHeartbeatsSinceCheck);

            string streamKey = $"Client:{tcp.DestinationPort}";
            long arrivedAt = new DateTimeOffset(raw.Timestamp, TimeSpan.Zero)
                                   .ToUnixTimeMilliseconds();

            uint currentSeq = tcp.SequenceNumber;
            uint payloadLen = (uint)tcp.PayloadData.Length;

            uint virtualLen = payloadLen;
            if (tcp.Synchronize) virtualLen++;
            if (tcp.Finished) virtualLen++;

            uint nextSeq = currentSeq + virtualLen;

            lock (expectedSeqNumbers)
            {
                if (!expectedSeqNumbers.TryGetValue(streamKey, out uint expectedSeq))
                {
                    expectedSeqNumbers[streamKey] = nextSeq;
                    if (tcp.PayloadData.Length < 1) return;
                    tcpStreamBuffer.AddData(streamKey, tcp.PayloadData, arrivedAt);
                    return;
                }

                if (currentSeq < expectedSeq)
                {
                    if (nextSeq <= expectedSeq)
                    {
                        logger.LogTrace(
                            $"[TCP] Duplicate/Old ignored. Seq:{currentSeq} Exp:{expectedSeq}");
                        return;
                    }
                }

                if (currentSeq > expectedSeq)
                {
                    logger.LogWarning(
                        $"[TCP GAP] Expected:{expectedSeq} Got:{currentSeq} " +
                        $"Diff:{currentSeq - expectedSeq}");

                    expectedSeqNumbers[streamKey] = nextSeq;
                    tcpStreamBuffer.AddData(streamKey, tcp.PayloadData, arrivedAt);
                    return;
                }

                // currentSeq == expectedSeq — normal in-order delivery
                expectedSeqNumbers[streamKey] = nextSeq;
                if (tcp.PayloadData.Length < 1) return;
                tcpStreamBuffer.AddData(streamKey, tcp.PayloadData, arrivedAt);
            }
        }



        private void HandleDetectionPacket(AdapterContext ctx, TcpPacket tcp)
        {
            if (tcp.PayloadData.Length < HeartbeatPattern.Length) return;

            ReadOnlySpan<byte> payload = tcp.PayloadData;
            if (payload.IndexOf(HeartbeatPattern) < 0) return;

            int port = tcp.SourcePort;

            lock (detectionLock)
            {
                if (isLocked) return; // race: locked between queue enqueue and here

                if (!detectionHits.TryGetValue(ctx.Device.Name, out var portMap))
                {
                    portMap = new Dictionary<int, int>();
                    detectionHits[ctx.Device.Name] = portMap;
                }

                portMap.TryGetValue(port, out int hits);
                portMap[port] = ++hits;

                logger.LogDebug(
                    $"[DETECT] Pattern on {ctx.Kind} adapter '{ctx.Device.Name}' " +
                    $"port {port} — hits {hits}/{DetectionThreshold}");

                if (hits >= DetectionThreshold && !graceWindowActive)
                {
                    graceWindowActive = true;
                    logger.LogInformation(
                        $"[DETECT] Threshold reached on {ctx.Kind} '{ctx.Device.Name}' " +
                        $"port {port}. Starting {GraceWindowMs}ms grace window...");

                    graceWindowTimer = new Timer(
                        _ => OnGraceWindowExpired(),
                        null,
                        GraceWindowMs,
                        Timeout.Infinite);
                }
            }
        }


        private void OnGraceWindowExpired()
        {
            lock (detectionLock)
            {
                if (isLocked) return;

                graceWindowTimer?.Dispose();
                graceWindowTimer = null;

                var (winnerAdapter, winnerPort) = PickWinner();

                if (winnerAdapter == null)
                {
                    // Shouldn't happen — grace only starts after threshold, but be safe.
                    logger.LogWarning("[DETECT] Grace window expired but no winner found. Resetting.");
                    graceWindowActive = false;
                    detectionHits.Clear();
                    return;
                }

                CommitLock(winnerAdapter, winnerPort);
            }
        }

        /// <summary>
        /// Picks the best adapter+port from accumulated detection hits.
        /// Priority: Loopback > Physical.
        /// highest hit count wins.
        /// </summary>
        private (AdapterContext? adapter, int port) PickWinner()
        {
            AdapterContext? bestLoopback = null; int bestLoopbackPort = 0; int bestLoopbackHits = 0;
            AdapterContext? bestPhysical = null; int bestPhysicalPort = 0; int bestPhysicalHits = 0;

            foreach (var ctx in allAdapters)
            {
                if (!detectionHits.TryGetValue(ctx.Device.Name, out var portMap)) continue;

                foreach (var kv in portMap)
                {
                    int port = kv.Key;
                    int hits = kv.Value;
                    if (hits < DetectionThreshold) continue;

                    if (ctx.Kind == AdapterKind.Loopback && hits > bestLoopbackHits)
                    {
                        bestLoopback = ctx;
                        bestLoopbackPort = port;
                        bestLoopbackHits = hits;
                    }
                    else if (ctx.Kind == AdapterKind.Physical && hits > bestPhysicalHits)
                    {
                        bestPhysical = ctx;
                        bestPhysicalPort = port;
                        bestPhysicalHits = hits;
                    }
                }
            }

            if (bestLoopback != null)
            {
                logger.LogInformation(
                    $"[DETECT] Winner: LOOPBACK '{bestLoopback.Device.Name}' " +
                    $"port {bestLoopbackPort} ({bestLoopbackHits} hits)." +
                    (bestPhysical != null
                        ? $" Physical '{bestPhysical.Device.Name}' port {bestPhysicalPort} " +
                          $"also had {bestPhysicalHits} hits — loopback preferred."
                        : string.Empty));

                return (bestLoopback, bestLoopbackPort);
            }

            if (bestPhysical != null)
            {
                logger.LogInformation(
                    $"[DETECT] Winner: PHYSICAL '{bestPhysical.Device.Name}' " +
                    $"port {bestPhysicalPort} ({bestPhysicalHits} hits). " +
                    $"No loopback candidate found.");

                return (bestPhysical, bestPhysicalPort);
            }

            return (null, 0);
        }


        private void CommitLock(AdapterContext winner, int port)
        {
            isLocked = true;
            lockedAdapter = winner;
            lockedPort = port;
            graceWindowActive = false;

            string filter = $"tcp src port {port}";
            try
            {
                winner.Device.Filter = filter;
                logger.LogInformation(
                    $"[CAPTURE] !!! LOCKED !!! {winner.Kind} adapter " +
                    $"'{winner.Device.Name}' port {port}. " +
                    $"Kernel filter: \"{filter}\"");
            }
            catch (Exception ex)
            {
                logger.LogError($"[CAPTURE] Failed to apply kernel filter: {ex.Message}");
            }

            CloseAllExcept(winner);

            detectionHits.Clear();

            watchdogHeartbeatsSinceCheck = 0;
            watchdogTimer = new Timer(
                CheckWatchdog, null,
                WatchdogTimeoutMs,
                WatchdogTimeoutMs);
        }


        private void CheckWatchdog(object? _)
        {
            if (!isCapturing || !isLocked) return;

            int beats = Interlocked.Exchange(ref watchdogHeartbeatsSinceCheck, 0);
            if (beats > 0)
            {
                logger.LogDebug($"[WATCHDOG] OK — {beats} heartbeat packet(s) in last window.");
                return;
            }

            logger.LogWarning(
                $"[WATCHDOG] No heartbeat for {WatchdogTimeoutMs / 1000}s on " +
                $"'{lockedAdapter?.Device.Name}' port {lockedPort}. " +
                $"Resetting to detection mode.");

            ResetToDetection();
        }


        private void ResetToDetection()
        {
            watchdogTimer?.Dispose(); watchdogTimer = null;
            graceWindowTimer?.Dispose(); graceWindowTimer = null;

            lock (detectionLock)
            {
                isLocked = false;
                graceWindowActive = false;
                lockedAdapter = null;
                lockedPort = 0;
                detectionHits.Clear();
            }

            lock (expectedSeqNumbers)
            {
                expectedSeqNumbers.Clear();
            }

            tcpStreamBuffer.Clear(); 

            appDroppedPackets = 0;

            foreach (var ctx in allAdapters)
            {
                if (!ctx.IsOpen)
                    OpenAdapterForDetection(ctx);
                else
                {
                    try { ctx.Device.Filter = "tcp"; }
                    catch { /* ignored */ }
                }
            }

            logger.LogInformation("[CAPTURE] Detection mode re-started on all adapters.");
        }

        private void ResetAllState()
        {
            lock (detectionLock)
            {
                isLocked = false;
                graceWindowActive = false;
                lockedAdapter = null;
                lockedPort = 0;
                detectionHits.Clear();
            }

            lock (expectedSeqNumbers)
            {
                expectedSeqNumbers.Clear();
            }

            appDroppedPackets = 0;
            watchdogHeartbeatsSinceCheck = 0;
        }


        private void LogDriverStats(object? _)
        {
            if (!isCapturing) return;
            foreach (var ctx in allAdapters)
            {
                if (!ctx.IsOpen) continue;
                try
                {
                    var s = ctx.Device.Statistics;
                    logger.LogDebug(
                        $"[STATS] {ctx.Kind} '{ctx.Device.Name}' — " +
                        $"Recv:{s.ReceivedPackets} DriverDrops:{s.DroppedPackets} " +
                        $"AppDrops:{appDroppedPackets}");
                }
                catch { /* ignored */ }
            }
        }
    }

}