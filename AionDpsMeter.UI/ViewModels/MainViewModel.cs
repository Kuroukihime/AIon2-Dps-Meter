using System.Windows;
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
            try
            {
                _packetService.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to start the Npcap service.\n\n" +
                    "Make sure Npcap is installed. To install Npcap:\n" +
                    "1. Download and install Npcap from https://npcap.com.\n" +
                    "2. During installation, you must check \"Install Npcap in WinPcap API-compatible Mode\".\n" +
                    "The meter will not work without this option enabled.",
                    "Service Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );


                Application.Current.Shutdown();
            }
        }

        public void Dispose()
        {
            _packetService.Stop();
            if (_packetService is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
