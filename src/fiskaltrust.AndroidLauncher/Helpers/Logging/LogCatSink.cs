using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using System;
using System.IO;

namespace fiskaltrust.AndroidLauncher.Helpers.Logging
{
    internal class LogcatSink : ILogEventSink
    {
        private readonly string _tag;
        private readonly ITextFormatter _formatter;
        private readonly LogEventLevel _restrictedToMinimumLevel;

        public LogcatSink(string tag, LogLevel restrictedToMinimumLevel)
        {
            _tag = tag ?? throw new ArgumentNullException(nameof(tag));

            _formatter = new MessageTemplateTextFormatter("{Message:lj}{NewLine}{Exception}");
            _restrictedToMinimumLevel = GetSerilogLogLevel(restrictedToMinimumLevel);
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent.Level < _restrictedToMinimumLevel)
            {
                return;
            }

            using var writer = new StringWriter();

            _formatter.Format(logEvent, writer);

            var msg = writer.ToString();

            switch (logEvent.Level)
            {
                case LogEventLevel.Verbose:
                    Android.Util.Log.Verbose(_tag, msg);
                    break;
                case LogEventLevel.Debug:
                    Android.Util.Log.Debug(_tag, msg);
                    break;
                case LogEventLevel.Information:
                    Android.Util.Log.Info(_tag, msg);
                    break;
                case LogEventLevel.Warning:
                    Android.Util.Log.Warn(_tag, msg);
                    break;
                case LogEventLevel.Fatal:
                case LogEventLevel.Error:
                    Android.Util.Log.Error(_tag, msg);
                    break;
            }
        }

        private LogEventLevel GetSerilogLogLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    return LogEventLevel.Verbose;
                case LogLevel.Debug:
                    return LogEventLevel.Debug;
                case LogLevel.Information:
                    return LogEventLevel.Information;
                case LogLevel.Warning:
                    return LogEventLevel.Warning;
                case LogLevel.Error:
                    return LogEventLevel.Error;
                case LogLevel.Critical:
                    return LogEventLevel.Fatal;
            }

            return LogEventLevel.Information;
        }
    }
}