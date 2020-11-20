using fiskaltrust.AndroidLauncher.Common.Helpers.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;

namespace fiskaltrust.AndroidLauncher.Common.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLogProviders(this IServiceCollection serviceCollection, LogLevel logLevel)
        {
            return serviceCollection.AddLogging(builder =>
            {
                builder.Services.AddSingleton<ILoggerProvider, AndroidLoggerProvider>();
                builder.Services.AddSingleton<ILoggerProvider, FileLoggerProvider>();
                builder.SetMinimumLevel(logLevel);
            });
        }
    }
}