using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.PacketProcessing.Shared;
using Microsoft.Extensions.Logging;

namespace AionDpsMeter.Services.PacketProcessing.Processors.PlayerEntity
{
  
    internal class PlayerStatsProcessor
    {
        private readonly ILogger<PlayerStatsProcessor> logger;

        public PlayerStatsProcessor(ILogger<PlayerStatsProcessor> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

    
        public void ProcessPlayerStats(byte[] packet)
        {
            try
            {
                var stats = Parse(packet);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to parse player stats packet ({Length} bytes)", packet.Length);
             
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
