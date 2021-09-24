using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace fiskaltrust.AndroidLauncher.Common.Helpers.Logging
{
    public sealed class FileLogger : ILogger
    {
        public const string LogFilename = "fiskaltrust.log";
        public const string LogDirectory = "logs";

        private static readonly Lazy<FileLogger> lazyInstance = new Lazy<FileLogger>(() => new FileLogger());
        private readonly Logger _log;

        static readonly LogEventProperty[] LowEventIdValues = Enumerable.Range(0, 48)
            .Select(n => new LogEventProperty("Id", new ScalarValue(n)))
            .ToArray();
        
        public static FileLogger Instance => lazyInstance.Value;

        static readonly CachingMessageTemplateParser MessageTemplateParser = new CachingMessageTemplateParser();
        
        private FileLogger() 
        {
            var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), LogDirectory);
            Directory.CreateDirectory(logDirectory);
            
            _log = new LoggerConfiguration()
                .WriteTo.File(Path.Combine(logDirectory, LogFilename), fileSizeLimitBytes: 5 * 1024, retainedFileCountLimit: 2, rollOnFileSizeLimit:true)
                .CreateLogger();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var level = LevelConvert.ToSerilogLevel(logLevel);
            if (!_log.IsEnabled(level))
            {
                return;
            }

            try
            {
                PrivateLog(level, eventId, state, exception, formatter);
            }
            catch (Exception ex)
            {
                //ToDo
            }
        }

        private void PrivateLog<TState>(LogEventLevel level, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var logger = _log;
            string messageTemplate = null;

            var properties = new List<LogEventProperty>();

            if (state is IEnumerable<KeyValuePair<string, object>> structure)
            {
                foreach (var property in structure)
                {
                    if (property.Key == "{OriginalFormat}" && property.Value is string value)
                    {
                        messageTemplate = value;
                    }
                    else if (property.Key.StartsWith("@"))
                    {
                        if (logger.BindProperty(property.Key.Substring(1), property.Value, true, out var destructured))
                            properties.Add(destructured);
                    }
                    else if (property.Key.StartsWith("$"))
                    {
                        if (logger.BindProperty(property.Key.Substring(1), property.Value?.ToString(), true, out var stringified))
                            properties.Add(stringified);
                    }
                    else
                    {
                        if (logger.BindProperty(property.Key, property.Value, false, out var bound))
                            properties.Add(bound);
                    }                    
                }

                var stateType = state.GetType();
                var stateTypeInfo = stateType.GetTypeInfo();
                if (messageTemplate == null && !stateTypeInfo.IsGenericType)
                {
                    messageTemplate = "{" + stateType.Name + ":l}";
                    if (logger.BindProperty(stateType.Name, AsLoggableValue(state, formatter), false, out var stateTypeProperty))
                        properties.Add(stateTypeProperty);
                }
            }

            if (messageTemplate == null)
            {
                string propertyName = null;
                if (state != null)
                {
                    propertyName = "State";
                    messageTemplate = "{State:l}";
                }
                else if (formatter != null)
                {
                    propertyName = "Message";
                    messageTemplate = "{Message:l}";
                }

                if (propertyName != null)
                {
                    if (logger.BindProperty(propertyName, AsLoggableValue(state, formatter), false, out var property))
                        properties.Add(property);
                }
            }

            if (eventId.Id != 0 || eventId.Name != null)
                properties.Add(CreateEventIdProperty(eventId));

            var parsedTemplate = MessageTemplateParser.Parse(messageTemplate ?? "");
            var evt = new LogEvent(DateTimeOffset.Now, level, exception, parsedTemplate, properties);
            logger.Write(evt);
        }
        
        static object AsLoggableValue<TState>(TState state, Func<TState, Exception, string> formatter)
        {
            object sobj = state;
            if (formatter != null)
                sobj = formatter(state, null);
            return sobj;
        }

        private static LogEventProperty CreateEventIdProperty(EventId eventId)
        {
            var properties = new List<LogEventProperty>(2);

            if (eventId.Id != 0)
            {
                if (eventId.Id >= 0 && eventId.Id < LowEventIdValues.Length)
                    properties.Add(LowEventIdValues[eventId.Id]);
                else
                    properties.Add(new LogEventProperty("Id", new ScalarValue(eventId.Id)));
            }

            if (eventId.Name != null)
            {
                properties.Add(new LogEventProperty("Name", new ScalarValue(eventId.Name)));
            }

            return new LogEventProperty("EventId", new StructureValue(properties));
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable BeginScope<TState>(TState state) => new NoopDisposable();
    }
}