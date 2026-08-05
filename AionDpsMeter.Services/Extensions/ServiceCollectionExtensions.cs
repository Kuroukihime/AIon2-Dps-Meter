using AionDpsMeter.Services.Services.Session.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AionDpsMeter.Services.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCombatHistoryPersistence(this IServiceCollection services, string sqlitePath)
        {
            var fullPath = Path.GetFullPath(sqlitePath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            services.AddDbContextFactory<CombatHistoryDbContext>(options =>
            {
                options.UseSqlite($"Data Source={fullPath}");
            });

            services.AddSingleton<ICombatHistoryStore, CombatHistoryStore>();
            return services;
        }
    }
}
