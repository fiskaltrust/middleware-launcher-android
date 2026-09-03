using Microsoft.ApplicationInsights.Extensibility;
using Serilog;
using Serilog.Events;

namespace fiskaltrust.AndroidLauncher.Helpers.Logging
{
    internal static class LauncherLogging
    {
        public const Microsoft.Extensions.Logging.LogLevel DefaultLogLevel = Microsoft.Extensions.Logging.LogLevel.Information;

        public static void InitializeDefault()
        {
            Log.Logger = CreateBaseConfiguration(DefaultLogLevel).CreateLogger();
        }

        public static void InitializeWithTelemetry(TelemetryConfiguration telemetryConfiguration, Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            Log.Logger = CreateBaseConfiguration(logLevel)
                .WriteTo.ApplicationInsights(telemetryConfiguration, TelemetryConverter.Traces, restrictedToMinimumLevel: LogEventLevel.Warning)
                .CreateLogger();
        }

        private static LoggerConfiguration CreateBaseConfiguration(Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            return new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .WriteTo.File(path: System.IO.Path.Combine(FileLoggerHelper.LogDirectory.FullName, FileLoggerHelper.LogFilename), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 31, shared: true)
                .WriteTo.Sink(new LogcatSink(AndroidLogger.TAG, logLevel));
        }
    }
}
