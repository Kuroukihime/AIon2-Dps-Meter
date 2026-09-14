using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.Services.Services.Update;
using AionDpsMeter.UI.Pages;
using AionDpsMeter.UI.Services.UiCommands;
using AionDpsMeter.UI.Utils;
using AionDpsMeter.UI.ViewModels;
using Microsoft.AspNetCore.Components.WebView.Wpf;

namespace AionDpsMeter.UI.Views
{
    public partial class MainWindow : Window
    {
        private readonly SettingsViewModel settingsViewModel;
        private readonly IAppSettingsService settingsService;
        private readonly UpdateCheckerService updateCheckerService;
        private SettingsWindow? settingsWindow;
        private HistoryWindow? historyWindow;
        private StatEfficiencyCalculatorWindow? statEfficiencyCalculatorWindow;
        private StatEfficiencyCalculatorViewModel? statEfficiencyCalculatorViewModel;
        private DispatcherTimer? _saveBoundsTimer;
        private GlobalHotkey? _globalHotkey;
        private readonly IUiCommandService _uiCommandService;

        public MainWindow(MainViewModel viewModel, SettingsViewModel settingsViewModel, IAppSettingsService settingsService, UpdateCheckerService updateCheckerService, IUiCommandService uiCommandService)
        {
            InitializeComponent();
            DataContext = viewModel;
            this.settingsViewModel    = settingsViewModel;
            this.settingsService      = settingsService;
            this.updateCheckerService = updateCheckerService;
            _uiCommandService = uiCommandService;

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
            Loaded += (_, _) => InitializeStyle3WebView();
            _uiCommandService.CommandRequested += OnUiCommandRequested;
            ApplyDisplayStyle();
        }

        private void InitializeStyle3WebView()
        {
            Style3WebView.WebView.DefaultBackgroundColor = System.Drawing.Color.Transparent;
            Style3WebView.Services = App.AppHost.Services;
            Style3WebView.RootComponents.Clear();
            Style3WebView.RootComponents.Add(new RootComponent
            {
                Selector = "#app",
                ComponentType = typeof(MainDpsMinimal)
            });
        }

        private void OnUiCommandRequested(object? sender, UiCommandRequest request)
        {
            Dispatcher.Invoke(() =>
            {
                switch (request.Command)
                {
                    case UiCommandType.BeginMainWindowDrag:
                        if (settingsService.UiStyle == 2)
                        {
                            try
                            {
                                DragMove();
                            }
                            catch
                            {
                            }
                        }
                        break;
                    case UiCommandType.MinimizeMainWindow:
                        WindowState = WindowState.Minimized;
                        break;
                    case UiCommandType.CloseApplication:
                        CloseButton_Click(this, new RoutedEventArgs());
                        break;
                    case UiCommandType.OpenSettings:
                        SettingsButton_Click(this, new RoutedEventArgs());
                        break;
                    case UiCommandType.OpenStatEff:
                        StatEfficiencyCalculatorButton_Click(this, new RoutedEventArgs());
                        break;
                    case UiCommandType.OpenHistory:
                        HistoryButton_Click(this, new RoutedEventArgs());
                        break;
                    case UiCommandType.OpenWhatsNew:
                        WhatsNewButton_Click(this, new RoutedEventArgs());
                        break;
                    case UiCommandType.OpenPlayerDetails:
                        if (DataContext is not MainViewModel viewModel || request.PlayerId is null)
                            return;

                        var player = viewModel.Players.FirstOrDefault(p => p.PlayerId == request.PlayerId);
                        if (player is null)
                            return;

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
                        break;
                }
            });
        }

        private void ApplyDisplayStyle()
        {
            if (DataContext is not MainViewModel vm) return;

            vm.NotifyDisplayStyleChanged();

         
            Style3WebView.Opacity = settingsService.UiStyle == 2
                ? settingsService.WindowOpacity
                : 1;

          
            if (settingsService.UiStyle is 1 or 2)
            {
                MainBorder.Background = Brushes.Transparent;
                MainBorder.BorderThickness = new Thickness(0);
                MainBorder.Opacity = 1;

                Style1Layout.ClearBackgroundImage();
            }
            else
            {
                MainBorder.Background = (Brush)FindResource("PrimaryBackgroundBrush");
                MainBorder.BorderThickness = new Thickness(0);  
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
                Style1Layout.ClearBackgroundImage();
                MainBorder.Background = (Brush)FindResource("PrimaryBackgroundBrush");
                return;
            }

            try
            {
                Style1Layout.SetBackgroundImage(path);
                MainBorder.Background = Brushes.Transparent;
            }
            catch
            {
                Style1Layout.ClearBackgroundImage();
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
            if (settingsService.UiStyle == 2)
                return;

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

            historyWindow = new HistoryWindow(viewModel.SessionManager, settingsService)
            {
                DataContext = new AionDpsMeter.UI.ViewModels.History.HistoryViewModel(viewModel.SessionManager, settingsService),
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

        private void StatEfficiencyCalculatorButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel viewModel) return;

            statEfficiencyCalculatorViewModel ??= new StatEfficiencyCalculatorViewModel(settingsService);
            statEfficiencyCalculatorViewModel.LoadFromSnapshot(viewModel.SessionManager.GetCurrentPlayerStatSnapshot());

            if (statEfficiencyCalculatorWindow is { IsVisible: true })
            {
                statEfficiencyCalculatorWindow.Activate();
                return;
            }

            statEfficiencyCalculatorWindow = new StatEfficiencyCalculatorWindow
            {
                DataContext = statEfficiencyCalculatorViewModel,
                Owner = this,
            };

            PositionWindowToRight(statEfficiencyCalculatorWindow);
            statEfficiencyCalculatorWindow.Show();
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
            _uiCommandService.CommandRequested -= OnUiCommandRequested;
            _saveBoundsTimer?.Stop();
            _saveBoundsTimer = null;
            SaveWindowBounds();
            if (DataContext is MainViewModel viewModel)
                viewModel.Dispose();
            base.OnClosed(e);
        }
    }
}