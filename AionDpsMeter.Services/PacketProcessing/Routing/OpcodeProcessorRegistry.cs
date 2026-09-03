using Microsoft.Extensions.Logging;

namespace AionDpsMeter.Services.PacketProcessing.Routing
{
    public sealed class OpcodeProcessorRegistry
    {
        private readonly Dictionary<ushort, IOpcodeProcessor> map;

        public OpcodeProcessorRegistry(IEnumerable<IOpcodeProcessor> processors, ILogger<OpcodeProcessorRegistry> logger)
        {
            map = BuildMap(processors);
            logger.LogInformation("Registered {Count} opcode processor mappings.", map.Count);
        }

        public bool TryGet(ushort opcode, out IOpcodeProcessor processor)
            => map.TryGetValue(opcode, out processor!);

        private static Dictionary<ushort, IOpcodeProcessor> BuildMap(IEnumerable<IOpcodeProcessor> processors)
        {
            var result = new Dictionary<ushort, IOpcodeProcessor>();

            foreach (var processor in processors)
            {
                var attrs = processor.GetType()
                    .GetCustomAttributes(typeof(PacketOpcodeAttribute), false)
                    .Cast<PacketOpcodeAttribute>()
                    .ToArray();

                if (attrs.Length == 0)
                    throw new InvalidOperationException($"Processor '{processor.GetType().Name}' must define at least one [PacketOpcode].");

                foreach (var attr in attrs)
                {
                    if (!result.TryAdd(attr.Opcode, processor))
                        throw new InvalidOperationException($"Duplicate opcode mapping detected for 0x{attr.Opcode:X4}.");
                }
            }

            return result;
        }
    }
}