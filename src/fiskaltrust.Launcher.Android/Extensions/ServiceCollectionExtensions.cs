using fiskaltrust.AndroidLauncher.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.AndroidLauncher
{
    public static class ServiceCollectionExtensions
    {
        public static void AddLogCatLogging(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddLogging(builder =>
            {
                builder.Services.AddSingleton<ILoggerProvider, AndroidLoggerProvider>();
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
        }
    }
}