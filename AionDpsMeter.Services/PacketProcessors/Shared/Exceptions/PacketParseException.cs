namespace AionDpsMeter.Services.PacketProcessors.Shared.Exceptions
{
    public sealed class PacketParseException : Exception
    {
        public int Offset { get; }
        public PacketParseException(string message, int offset) : base($"{message} (offset {offset})")
        {
            Offset = offset;
        }
    }
}
