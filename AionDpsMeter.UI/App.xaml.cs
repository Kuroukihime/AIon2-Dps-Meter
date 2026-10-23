using AionDpsMeter.Services.Extensions;
using AionDpsMeter.Services.Models;
using AionDpsMeter.Services.PacketCapture;
using AionDpsMeter.Services.Services;
using AionDpsMeter.Services.Services.Entity;
using AionDpsMeter.Services.Services.Session;
using AionDpsMeter.Services.Services.Session.Persistence;
using AionDpsMeter.Services.Services.Settings;
using AionDpsMeter.Services.Services.Timed;
using AionDpsMeter.Services.Services.Update;
using AionDpsMeter.Core.Windowing;
using AionDpsMeter.UI.Services.Windowing;
using AionDpsMeter.UI.ViewModels;
using AionDpsMeter.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Windows;

namespace AionDpsMeter.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IHost AppHost { get; private set; }

        public App()
        {
            ConfigureWebView2Arguments();


            AppHost = Host.CreateDefaultBuilder()
                .UseSerilog((context, services, loggerConfiguration) =>
                {
                    loggerConfiguration
                        .ReadFrom.Configuration(context.Configuration);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddCombatHistoryPersistence("combat-history.db");
                   
                    services.AddSingleton<IAppSettingsService, AppSettingsService>();
                    services.AddSingleton<UpdateCheckerService>();
                    services.AddSingleton<FilePacketWriter>();
                    services.AddSingleton<TcpStreamBuffer>();

                    //services.AddSingleton<IPacketCaptureDevice, FilePacketCaptureDevice>();
                    services.AddSingleton<IPacketCaptureDevice, CaptureDevice>();

                    services.AddSingleton<EntityTracker>();
                    services.AddKeyedSingleton<ITimedEventTracker, BuffTimedEventTracker>("Buffs");
                    services.AddKeyedSingleton<ITimedEventTracker, SkillCdTimedEventTracker>("SkillCdUpdate");
                    services.AddSingleton<CombatSessionManager>();
                    services.AddPacketProcessingRouting();
                    services.AddSingleton<IPacketService, PacketPipelineService>();

                    services.AddWindowManager();


                    services.AddSingleton<SettingsViewModel>();
                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<SettingsWindow>();
                    services.AddWpfBlazorWebView();

                })
                .Build();
        }

        private static void ConfigureWebView2Arguments()
        {
            const string requiredArguments = "--disable-features=BackgroundHtmlParserBudget,CalculateNativeWinOcclusion,IntensiveWakeUpThrottling --disable-backgrounding-occluded-windows --disable-renderer-backgrounding";

            var existingArguments = Environment.GetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", EnvironmentVariableTarget.Process);
            var combinedArguments = string.IsNullOrWhiteSpace(existingArguments)
                ? requiredArguments
                : $"{existingArguments} {requiredArguments}";

            Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", combinedArguments, EnvironmentVariableTarget.Process);
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await AppHost.StartAsync();

            _ = AppHost.Services.GetRequiredService<ICombatHistoryStore>();

            var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
            var windowManager = AppHost.Services.GetRequiredService<IWindowManagerService>();
            var windowHelper = AppHost.Services.GetRequiredService<WindowHelper>();
            windowManager.Open(WindowKey.Main, mainWindow, true);
            windowHelper.OpenRequiredWindows();
            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            try
            {
                var sessionManager = AppHost.Services.GetRequiredService<CombatSessionManager>();
                var historyStore = AppHost.Services.GetRequiredService<ICombatHistoryStore>();
                sessionManager.CompleteAndPersistActiveSessions();
                historyStore.FlushPendingSaves();
                await AppHost.StopAsync();
                await Log.CloseAndFlushAsync();
                base.OnExit(e);
            }
            catch (Exception ex)
            {
                base.OnExit(e);
            }
        }
    }

}
