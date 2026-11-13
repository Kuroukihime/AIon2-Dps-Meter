using System.Diagnostics;
using AionDpsMeter.Core.GameData.Services;
using AionDpsMeter.Services.PacketProcessing.Routing;
using AionDpsMeter.Services.PacketProcessing.Shared;
using AionDpsMeter.Services.Services.Session;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.Services.PacketProcessing.Processors
{
    [PacketOpcode(PacketOpcodes.BuffRemoved)]
    public sealed class BuffRemovedPacketProcessor : IOpcodeProcessor
    {



        private readonly GameDataProvider gameData;
        private readonly ILogger<BuffRemovedPacketProcessor> logger;
        private readonly CombatSessionManager sessionManager;

        public BuffRemovedPacketProcessor(ILogger<BuffRemovedPacketProcessor> logger, CombatSessionManager sessionManager)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.gameData = GameDataProvider.Instance;
            this.sessionManager = sessionManager;

        }

        public void Process(Packet packet)
        {
            var r = new PacketReader(packet.Data);

            r.ReadVarInt(); //len
            r.ReadU16();    //opcode

            var actorId = r.ReadVarInt();
            var count = r.ReadVarInt();

            for (int i = 0; i < count; i++)
            {
                var mask = r.ReadU8();
                var castId = r.ReadVarInt();
                r.ReadU8();
                if ((mask & 0x01) != 0) r.ReadVarInt();
                if ((mask & 0x02) != 0) r.ReadU32();
                if ((mask & 0x04) != 0) r.ReadU32();

                sessionManager.ProcessBuffRemoveEvent((int)actorId, (int)castId);
            }
        }


      


    }
}
