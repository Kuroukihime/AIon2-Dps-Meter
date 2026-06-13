using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.Services.Services.Update;
using AionDpsMeter.UI.Utils;
using AionDpsMeter.UI.ViewModels;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace AionDpsMeter.UI
{
    public partial class MainWindow : Window
    {
        private readonly SettingsViewModel settingsViewModel;
        private readonly IAppSettingsService settingsService;
        private readonly UpdateCheckerService updateCheckerService;
        private SettingsWindow? settingsWindow;
        private HistoryWindow? historyWindow;
        private DispatcherTimer? _saveBoundsTimer;
        private GlobalHotkey? _globalHotkey;

        public MainWindow(MainViewModel viewModel, SettingsViewModel settingsViewModel, IAppSettingsService settingsService, UpdateCheckerService updateCheckerService)
        {
            InitializeComponent();
            DataContext = viewModel;
            this.settingsViewModel    = settingsViewModel;
            this.settingsService      = settingsService;
            this.updateCheckerService = updateCheckerService;

            _saveBoundsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _saveBoundsTimer.Tick += (_, _) => { _saveBoundsTimer.Stop(); SaveWindowBounds(); };

            RestoreWindowBounds();

            MainBorder.Opacity = settingsService.WindowOpacity;
            ApplyBackgroundImage(settingsService.BackgroundImagePath);

            settingsService.SettingsChanged += (_, _) =>
                Dispatcher.InvokeAsync(() =>
                {
                    MainBorder.Opacity = settingsService.WindowOpacity;
                    ApplyBackgroundImage(settingsService.BackgroundImagePath);
                    RegisterToggleHotkey();
                    ApplyDisplayStyle();
                });

            Loaded += (_, _) => RegisterToggleHotkey();
            ApplyDisplayStyle();
        }
        private void ApplyDisplayStyle()
        {
            if (DataContext is not MainViewModel vm) return;

            vm.NotifyDisplayStyleChanged();

          
            if (settingsService.UiStyle == 1)
            {
                // Style 2: fully transparent window — game renders behind it
                MainBorder.Background = Brushes.Transparent;
                MainBorder.BorderThickness = new Thickness(0);

                // Hide the background image; Style 2 has no backdrop
                PlayersBackgroundImage.Source = null;
                PlayersBackgroundOverlay.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Style 1: restore normal dark background + opacity
                MainBorder.Background = (Brush)FindResource("PrimaryBackgroundBrush");
                MainBorder.BorderThickness = new Thickness(0);   // keep your original value
                MainBorder.Opacity = settingsService.WindowOpacity;
                ApplyBackgroundImage(settingsService.BackgroundImagePath);
            }
        }
        private void RegisterToggleHotkey()
        {
            _globalHotkey ??= new GlobalHotkey(this);
            _globalHotkey.HotkeyPressed -= ToggleWindowVisibility;
            _globalHotkey.Unregister();

            var (mods, vk) = HotkeyParser.Parse(settingsService.ToggleVisibilityHotkey);
            if (vk != 0)
            {
                _globalHotkey.Register(mods, vk);
                _globalHotkey.HotkeyPressed += ToggleWindowVisibility;
            }
        }

        private void ToggleWindowVisibility()
        {
            var appWindows = Application.Current.Windows.OfType<Window>().ToList();

            if (WindowState != WindowState.Minimized)
            {
                foreach (var window in appWindows)
                    window.WindowState = WindowState.Minimized;
            }
            else
            {
                foreach (var window in appWindows)
                {
                    window.WindowState = WindowState.Normal;
                    window.Activate();
                }
                Activate();
            }
        }

        private void ApplyBackgroundImage(string? path)
        {
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
            {
                PlayersBackgroundImage.Source = null;
                PlayersBackgroundOverlay.Visibility = Visibility.Collapsed;
                MainBorder.Background = (Brush)FindResource("PrimaryBackgroundBrush");
                return;
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                PlayersBackgroundImage.Source = bitmap;
                PlayersBackgroundOverlay.Visibility = Visibility.Visible;
                MainBorder.Background = Brushes.Transparent;
            }
            catch
            {
                PlayersBackgroundImage.Source = null;
                PlayersBackgroundOverlay.Visibility = Visibility.Collapsed;
                MainBorder.Background = (Brush)FindResource("PrimaryBackgroundBrush");
            }
        }

        private void RestoreWindowBounds()
        {
            var left   = settingsService.WindowLeft;
            var top    = settingsService.WindowTop;
            var width  = settingsService.WindowWidth;
            var height = settingsService.WindowHeight;

            if (!left.HasValue || !top.HasValue)
                return;

            double w = width.HasValue  ? Math.Max(MinWidth,  width.Value)  : Width;
            double h = height.HasValue ? Math.Max(MinHeight, height.Value) : Height;

            var wa = ScreenHelper.GetWorkingAreaForPoint(left.Value, top.Value);

            double l = Math.Max(wa.Left, Math.Min(left.Value, wa.Right  - w));
            double t = Math.Max(wa.Top,  Math.Min(top.Value,  wa.Bottom - h));

            WindowStartupLocation = WindowStartupLocation.Manual;
            Left   = l;
            Top    = t;
            Width  = w;
            Height = h;
        }

      
        private void SaveWindowBounds()
        {
            if (WindowState != WindowState.Normal) return;
            settingsService.WindowLeft   = Left;
            settingsService.WindowTop    = Top;
            settingsService.WindowWidth  = Width;
            settingsService.WindowHeight = Height;
        }

        private void PositionWindowToRight(Window child)
        {
            const double gap = 8;

            var wa = ScreenHelper.GetWorkingAreaForWindow(this);

            double mainRight     = Left + Width;
            double candidateLeft = mainRight + gap;

            double childLeft;
            if (candidateLeft + child.Width <= wa.Right)
            {
                childLeft = candidateLeft;
            }
            else
            {
                childLeft = Math.Max(wa.Left, wa.Right - child.Width);
            }

            double childTop = Math.Max(wa.Top, Math.Min(Top, wa.Bottom - child.Height));

            child.WindowStartupLocation = WindowStartupLocation.Manual;
            child.Left = childLeft;
            child.Top  = childTop;
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            _saveBoundsTimer?.Stop();
            _saveBoundsTimer?.Start();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            _saveBoundsTimer?.Stop();
            _saveBoundsTimer?.Start();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel viewModel) return;

            // Singleton: bring existing window to front instead of opening a new one
            if (historyWindow is { IsVisible: true })
            {
                historyWindow.Activate();
                return;
            }

            var snapshot = viewModel.SessionManager.GetHistorySnapshot();

            historyWindow = new HistoryWindow(settingsService)
            {
                DataContext = new AionDpsMeter.UI.ViewModels.History.HistoryViewModel(snapshot, settingsService),
                Owner = this
            };

            PositionWindowToRight(historyWindow);
            historyWindow.Show();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (settingsWindow is { IsVisible: true })
            {
                settingsWindow.Activate();
                return;
            }

            settingsWindow = new SettingsWindow
            {
                DataContext = settingsViewModel,
                Owner = this
            };

            PositionWindowToRight(settingsWindow);
            settingsWindow.Show();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _globalHotkey?.Dispose();
            if (DataContext is MainViewModel viewModel)
                viewModel.Dispose();
            Application.Current.Shutdown();
        }

        private void WhatsNewButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel vm || vm.LatestRelease is null) return;

            var win = new WhatsNewWindow(vm.LatestRelease, updateCheckerService)
            {
                Owner = this
            };
            PositionWindowToRight(win);
            win.Show();
        }

        private void PlayerItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element &&
                element.Tag is PlayerStatsViewModel player &&
                DataContext is MainViewModel viewModel)
            {
                var detailsWindow = new PlayerDetailsWindow
                {
                    DataContext = new PlayerDetailsViewModel(
                        viewModel.SessionManager,
                        player.PlayerId,
                        player.PlayerName,
                        player.ClassName,
                        player.PlayerIcon,
                        player.ClassIcon,
                        settingsService,
                        player.CombatPower,
                        player.ServerName),
                    Owner = this
                };

                PositionWindowToRight(detailsWindow);
                detailsWindow.Show();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _saveBoundsTimer?.Stop();
            _saveBoundsTimer = null;
            SaveWindowBounds();
            if (DataContext is MainViewModel viewModel)
                viewModel.Dispose();
            base.OnClosed(e);
        }
    }
}