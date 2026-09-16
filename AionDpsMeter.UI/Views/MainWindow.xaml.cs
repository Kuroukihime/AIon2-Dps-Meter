using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using AionDpsMeter.Services.Services.Settings;
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

            settingsService.SettingsChanged += (_, _) =>
                Dispatcher.InvokeAsync(() =>
                {
                    MainBorder.Opacity = settingsService.WindowOpacity;
                    RegisterToggleHotkey();
                });

            Loaded += (_, _) => RegisterToggleHotkey();
            Loaded += (_, _) => InitializeStyle2WebView();
            windowManager.CloseAppCommand += OnCloseCommand;
        }

        private void OnCloseCommand(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() => { CloseButton_Click(this, new RoutedEventArgs()); });
        }

      

        private void InitializeStyle2WebView()
        {
            Style2WebView.WebView.DefaultBackgroundColor = System.Drawing.Color.Transparent;
            Style2WebView.Services = App.AppHost.Services;
            Style2WebView.RootComponents.Clear();
            Style2WebView.RootComponents.Add(new RootComponent
            {
                Selector = "#app",
                ComponentType = typeof(MainDpsPage)
            });
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


        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _globalHotkey?.Dispose();
            if (DataContext is MainViewModel viewModel)
                viewModel.Dispose();
            Application.Current.Shutdown();
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