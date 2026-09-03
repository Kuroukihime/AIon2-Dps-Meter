namespace AionDpsMeter.Services.PacketProcessing.Routing
{
    public interface IOpcodeProcessor
    {
        void Process(Packet packet);
    }
}
