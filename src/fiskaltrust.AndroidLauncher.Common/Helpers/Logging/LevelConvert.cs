using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace fiskaltrust.AndroidLauncher.Common.Helpers.Logging
{
    public static class LevelConvert
    {
        public static LogEventLevel ToSerilogLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.None:
                case LogLevel.Critical:
                    return LogEventLevel.Fatal;
                case LogLevel.Error:
                    return LogEventLevel.Error;
                case LogLevel.Warning:
                    return LogEventLevel.Warning;
                case LogLevel.Information:
                    return LogEventLevel.Information;
                case LogLevel.Debug:
                    return LogEventLevel.Debug;
                case LogLevel.Trace:
                default:
                    return LogEventLevel.Verbose;
            }
        }

        public static LogLevel ToExtensionsLevel(LogEventLevel logEventLevel)
        {
            switch (logEventLevel)
            {
                case LogEventLevel.Fatal:
                    return LogLevel.Critical;
                case LogEventLevel.Error:
                    return LogLevel.Error;
                case LogEventLevel.Warning:
                    return LogLevel.Warning;
                case LogEventLevel.Information:
                    return LogLevel.Information;
                case LogEventLevel.Debug:
                    return LogLevel.Debug;
                case LogEventLevel.Verbose:
                default:
                    return LogLevel.Trace;
            }
        }
    }
}