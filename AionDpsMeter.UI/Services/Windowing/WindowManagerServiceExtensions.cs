using Microsoft.Extensions.DependencyInjection;

namespace AionDpsMeter.UI.Services.Windowing
{
    public static class WindowManagerServiceExtensions
    {
        public static IServiceCollection AddWindowManager(this IServiceCollection services)
        {
             services.AddSingleton<IWindowManagerService, WindowManagerService>();
             services.AddSingleton<WindowHelper>();
             return services;
        }
    }
}
