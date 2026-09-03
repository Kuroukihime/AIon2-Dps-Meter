using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.PacketProcessing.Routing;
using AionDpsMeter.Services.PacketProcessing.Shared;
using AionDpsMeter.Services.Services.Session;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;

namespace AionDpsMeter.Services.PacketProcessing.Processors
{
    [PacketOpcode(PacketOpcodes.PlayerStats)]
    public class PlayerStatsProcessor : IOpcodeProcessor
    {
        private readonly CombatSessionManager sessionManager;
        private readonly ILogger<PlayerStatsProcessor> logger;

        public PlayerStatsProcessor(ILogger<PlayerStatsProcessor> logger, CombatSessionManager sessionManager)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.sessionManager = sessionManager;
        }

        public void Process(Packet packet)
        {
            try
            {
                var stats = Parse(packet.Data);
                sessionManager.ProcessPlayerStatsUpdate(stats, DateAndTime.Now);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to parse player stats packet ({Length} bytes)", packet.Data.Length);

            }
        }

    
        private static PlayerCharacterStats Parse(byte[] packet)
        {
            var reader = new PacketReader(packet);

            reader.ReadVarInt();      // packet length
            reader.ReadU16();         // opcode
            reader.ReadU8();          // unknown
            reader.ReadU8();          // unknown

            var count  = (int)reader.ReadVarInt();
            var values = new Dictionary<ushort, int>(count);

            for (int i = 0; i < count; i++)
            {
                ushort id = reader.ReadU16();
                int value = (int)reader.ReadU32();
                values[id] = value;
            }

            return new PlayerCharacterStats(values);
        }

       
    }
}
