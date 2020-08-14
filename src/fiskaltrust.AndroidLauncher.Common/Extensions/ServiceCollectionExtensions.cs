using fiskaltrust.AndroidLauncher.Common.Helpers.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.AndroidLauncher.Common.Extensions
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