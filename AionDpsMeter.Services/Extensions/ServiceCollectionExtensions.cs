using AionDpsMeter.Services.PacketProcessing;
using AionDpsMeter.Services.PacketProcessing.Routing;
using AionDpsMeter.Services.Services.Session.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AionDpsMeter.Services.Extensions
{
    public static class ServiceCollectionExtensions
    {
        extension(IServiceCollection services)
        {
            public IServiceCollection AddCombatHistoryPersistence(string sqlitePath)
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

            public IServiceCollection AddPacketProcessingRouting()
            {
                var assembly = typeof(PacketProcessor).Assembly;

                var processorTypes = assembly.GetTypes()
                    .Where(t =>
                        t is { IsClass: true, IsAbstract: false } &&
                        t.Namespace is not null &&
                        t.Namespace.Contains(".PacketProcessing.Processors"))
                    .ToArray();

                foreach (var processorType in processorTypes)
                {
                    services.AddSingleton(processorType);
                }

                foreach (var opcodeProcessorType in processorTypes.Where(t => typeof(IOpcodeProcessor).IsAssignableFrom(t)))
                {
                    services.AddSingleton(typeof(IOpcodeProcessor),
                        sp => (IOpcodeProcessor)sp.GetRequiredService(opcodeProcessorType));
                }

                services.AddSingleton<PacketProcessor>();
                services.AddSingleton<OpcodeProcessorRegistry>();
                services.AddSingleton<PacketDispatchService>();

                return services;
            }
        }
    }
}
