using fiskaltrust.AndroidLauncher.Common.Helpers.Logging;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using Xamarin.Essentials;

namespace fiskaltrust.AndroidLauncher.Common.Extensions
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

        public static IServiceCollection AddAppInsightsLogging(this IServiceCollection services, string instrumentationKey, string package, Guid cashBoxId, LogLevel verbosity)
        {
            var channel = new InMemoryChannel();
            {
                services.Configure<TelemetryConfiguration>(config =>
                {
                    config.TelemetryChannel = channel;
                    config.TelemetryInitializers.Add(new MiddlewareTelemetryInitializer(package, VersionTracking.CurrentVersion, cashBoxId));
                    new DependencyTrackingTelemetryModule().Initialize(config);
                });

                services.AddLogging(builder =>
                {
                    builder.SetMinimumLevel(verbosity);

                    builder.AddApplicationInsights(instrumentationKey);
                });
                services.AddSingleton<ITelemetryChannel>(channel);
                return services;
            }
        }

        public static IServiceCollection FlushAppInsightsLogging(this IServiceCollection services, bool useOffline)
        {
            if (useOffline)
            {
                return services;
            }
            var channels = services.BuildServiceProvider().GetServices<ITelemetryChannel>();
            foreach (var channel in channels)
            {
                channel.Flush();
                channel.Dispose();
            }

            return services;
        }
    }
}