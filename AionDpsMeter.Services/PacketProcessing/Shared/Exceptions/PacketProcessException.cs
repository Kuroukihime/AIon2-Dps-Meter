namespace AionDpsMeter.Services.PacketProcessing.Shared.Exceptions
{
    public sealed class PacketProcessException : Exception
    {
        public PacketProcessException(string message) : base($"{message}") { }
    }
}
