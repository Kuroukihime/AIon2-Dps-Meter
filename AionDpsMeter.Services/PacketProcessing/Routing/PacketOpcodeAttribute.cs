namespace AionDpsMeter.Services.PacketProcessing.Routing
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    internal sealed class PacketOpcodeAttribute(ushort opcode) : Attribute
    {
        public ushort Opcode { get; } = opcode;
    }
}