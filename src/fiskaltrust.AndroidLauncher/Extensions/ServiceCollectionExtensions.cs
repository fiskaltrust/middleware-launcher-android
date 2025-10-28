using fiskaltrust.AndroidLauncher.Helpers.Logging;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using Microsoft.Extensions.Logging.ApplicationInsights;

namespace fiskaltrust.AndroidLauncher.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLogProviders(this IServiceCollection serviceCollection, LogLevel logLevel)
        {
            return serviceCollection.AddLogging(builder =>
            {
                builder.AddSerilog();
                builder.Services.AddSingleton<ILoggerProvider, AndroidLoggerProvider>();
                builder.SetMinimumLevel(logLevel);
            });
        }

        public static IServiceCollection AddAppInsights(this IServiceCollection services, string instrumentationKey, string package, Guid cashBoxId)
        {
            var channel = new InMemoryChannel();
            services.Configure<TelemetryConfiguration>(config =>
            {
                config.TelemetryChannel = channel;
                config.TelemetryInitializers.Add(new MiddlewareTelemetryInitializer(package, VersionTracking.CurrentVersion, cashBoxId));
                new DependencyTrackingTelemetryModule().Initialize(config);
            });

            services.AddLogging(builder =>
            {
                builder.AddFilter<ApplicationInsightsLoggerProvider>("", LogLevel.Warning);
                builder.AddApplicationInsights(instrumentationKey);
            });
            services.AddSingleton<ITelemetryChannel>(channel);
            return services;
        }
    }
}