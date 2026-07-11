namespace AionDpsMeter.Services.PacketProcessors.Shared.Exceptions
{
    public sealed class PacketProcessException : Exception
    {
        public PacketProcessException(string message) : base($"{message}") { }
    }
}
