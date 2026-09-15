using AionDpsMeter.Services.Models;

namespace AionDpsMeter.UI.ViewModels
{
    public sealed partial class MainViewModel : ViewModelBase, IDisposable
    {
        private readonly IPacketService _packetService;


        public MainViewModel(IPacketService packetService)
        {
            _packetService = packetService;
            StartCapture();
        }


        private void StartCapture()
        {
            _packetService.Start();
        }

        public void Dispose()
        {
            _packetService.Stop();
            if (_packetService is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
