using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.Services.Services.Session;
using AionDpsMeter.UI.ViewModels.History;
using System.Windows;
using System.Windows.Input;

namespace AionDpsMeter.UI
{
    public partial class HistoryWindow : Window
    {
        private readonly CombatSessionManager _sessionManager;
        private readonly IAppSettingsService _settingsService;

        public HistoryWindow(CombatSessionManager sessionManager, IAppSettingsService settingsService)
        {
            InitializeComponent();
            _sessionManager = sessionManager;
            _settingsService = settingsService;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void SessionItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement el && el.Tag is HistoryEntryViewModel entry)
            {
                var snapshot = _sessionManager.GetHistorySession(entry.SessionId);
                if (snapshot is null) return;

                var sessionWindow = new HistorySessionWindow(_settingsService)
                {
                    DataContext = new HistorySessionViewModel(snapshot, _settingsService),
                    Owner = this
                };
                sessionWindow.Show();
            }
        }
    }
}
