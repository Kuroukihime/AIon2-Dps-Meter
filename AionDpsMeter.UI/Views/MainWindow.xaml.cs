using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.Services.Services.Update;
using AionDpsMeter.UI.Pages;
using AionDpsMeter.UI.Services.Windowing;
using AionDpsMeter.UI.Utils;
using AionDpsMeter.UI.ViewModels;
using Microsoft.AspNetCore.Components.WebView.Wpf;

namespace AionDpsMeter.UI.Views
{
    public partial class MainWindow : Window
    {
        private readonly IAppSettingsService settingsService;
        private DispatcherTimer? _saveBoundsTimer;
        private GlobalHotkey? _globalHotkey;
        private readonly IWindowManagerService windowManager;
        private readonly WindowHelper windowHelper;

        public MainWindow(MainViewModel viewModel, IAppSettingsService settingsService, IWindowManagerService windowManager, WindowHelper windowHelper)
        {
            InitializeComponent();
            DataContext = viewModel;
            this.settingsService      = settingsService;
            this.windowManager = windowManager;
            this.windowHelper = windowHelper;

            _saveBoundsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(5000) };
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
            windowManager.CloseAppCommand += OnCloseCommand;
            ApplyDisplayStyle();
        }

        private void OnCloseCommand(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() => { CloseButton_Click(this, new RoutedEventArgs()); });
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
                //MainBorder.Opacity = 1;

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


        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (settingsService.UiStyle == 2)
                return;

            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => windowManager.Minimize(WindowKey.Main); 

        private void HistoryButton_Click(object sender, RoutedEventArgs e) => windowHelper.OpenHistory();

        private void SettingsButton_Click(object sender, RoutedEventArgs e) => windowHelper.OpenSettings();

        private void StatEfficiencyCalculatorButton_Click(object sender, RoutedEventArgs e) => windowHelper.OpenStatEff();

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _globalHotkey?.Dispose();
            if (DataContext is MainViewModel viewModel)
                viewModel.Dispose();
            Application.Current.Shutdown();
        }

        private void WhatsNewButton_Click(object sender, RoutedEventArgs e) => windowHelper.OpenWhatsNewWindow();
       

        private void PlayerItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element &&
                element.Tag is PlayerStatsViewModel player &&
                DataContext is MainViewModel viewModel)
            {
                windowHelper.OpenPlayerDetails(new PlayerRenderState()
                {
                    ClassIcon = player.ClassIcon,
                    ClassId = player.ClassId.ToString(),
                    ClassName = player.ClassName,
                    CombatPower = player.CombatPower,
                    DamagePercentage = player.DamagePercentage,
                    DeathsDisplay = player.PlayerDeathsDisplay,
                    DpsFormatted = player.DpsFormatted,
                    EffectivePercentage = player.EffectivePercentage,
                    IsUser = player.IsUser,
                    PlayerId = player.PlayerId,
                    PlayerNameDisplay = player.PlayerNameDisplay,
                    ServerName = player.ServerName,
                    TotalDamage = player.TotalDamage,
                    TotalDamageFormatted = player.TotalDamageFormatted,
                    VisualAbsolutePercentage = player.AbsolutePercentage,
                    VisualRelativePercentage = player.RelativePercentage,
                });
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