using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.UI.Pages;
using AionDpsMeter.UI.Services.Tray;
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
        private readonly TrayService trayService;
        private ResizeGrip? _resizeGrip;

        public MainWindow(MainViewModel viewModel, IAppSettingsService settingsService, IWindowManagerService windowManager, WindowHelper windowHelper, TrayService trayService)
        {
            InitializeComponent();
            WebViewEnvironment.Configure(Style2WebView);
            DataContext = viewModel;
            this.settingsService      = settingsService;
            this.windowManager = windowManager;
            this.windowHelper = windowHelper;
            this.trayService = trayService;

            _saveBoundsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(5000) };
            _saveBoundsTimer.Tick += (_, _) => { _saveBoundsTimer.Stop(); SaveWindowSize(); };

            RestoreWindowSize();

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
            windowHelper.WindowStateUpdated += (_, _) => SetResizeGripVisible(windowHelper.IsMeterEdit);
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

        private void ToggleWindowVisibility() => trayService.Toggle();

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _resizeGrip = FindVisualChild<ResizeGrip>(this);
            SetResizeGripVisible(windowHelper.IsMeterEdit);
        }

        // The grip only shows while the meter accepts the mouse. Toggling the element, not ResizeMode,
        // avoids a window-style change that re-lays out the WebView and briefly blocks input.
        private void SetResizeGripVisible(bool visible)
        {
            if (_resizeGrip is null) return;
            _resizeGrip.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T match) return match;
                if (FindVisualChild<T>(child) is { } nested) return nested;
            }
            return null;
        }


        // Only the size is persisted; the position is game-relative (see WindowHelper.PlaceWindowsOverGame).
        private void RestoreWindowSize()
        {
            if (settingsService.WindowWidth is { } width)   Width  = Math.Max(MinWidth,  width);
            if (settingsService.WindowHeight is { } height) Height = Math.Max(MinHeight, height);
        }

        private void SaveWindowSize()
        {
            if (WindowState != WindowState.Normal) return;
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
            SaveWindowSize();
            if (DataContext is MainViewModel viewModel)
                viewModel.Dispose();
            base.OnClosed(e);
        }
    }
}