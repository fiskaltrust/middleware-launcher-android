using Microsoft.ApplicationInsights.Extensibility;
using Serilog;
using Serilog.Events;

namespace fiskaltrust.AndroidLauncher.Helpers.Logging
{
    internal static class LauncherLogging
    {
        public const Microsoft.Extensions.Logging.LogLevel DefaultLogLevel = Microsoft.Extensions.Logging.LogLevel.Information;

        private static readonly Lazy<Serilog.ILogger> FileLogger = new(() => new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.File(path: System.IO.Path.Combine(FileLoggerHelper.LogDirectory.FullName, FileLoggerHelper.LogFilename), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 31, hooks: new StartupHeaderHooks())
            .CreateLogger());

        public static void InitializeDefault()
        {
            Log.Logger = CreateBaseConfiguration(DefaultLogLevel).CreateLogger();
            Log.Logger.Information("fiskaltrust.AndroidLauncher process started.");
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
                .WriteTo.Logger(FileLogger.Value)
                .WriteTo.Sink(new LogcatSink(AndroidLogger.TAG, logLevel));
        }
    }
}
