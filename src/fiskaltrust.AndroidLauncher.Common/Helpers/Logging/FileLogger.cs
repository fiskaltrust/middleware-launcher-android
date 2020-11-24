using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Common.Helpers.Logging
{
    internal sealed class FileLogger : ILogger
    {
        private const string LOG_FILENAME = "fiskaltrust.log";

        private static readonly Lazy<FileLogger> lazyInstance = new Lazy<FileLogger>(() => new FileLogger());
        private readonly BlockingCollection<string> _logQueue;
        private readonly string _logDirectory;
        
        public static FileLogger Instance => lazyInstance.Value;

        private FileLogger() 
        {
            _logQueue = new BlockingCollection<string>();
            _logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "log");
            Directory.CreateDirectory(_logDirectory);
            
            Task.Run(() => ProcessQueue());
        }

        private void ProcessQueue()
        {
            while (!_logQueue.IsCompleted)
            {
                var line = _logQueue.Take();
                File.AppendAllLines(Path.Combine(_logDirectory, LOG_FILENAME), new[] { line });
            }
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var message = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{logLevel}] {state}";
            if(exception != null)
            {
                message += $"\n{exception}";
            }
            _logQueue.Add(message);
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable BeginScope<TState>(TState state) => new NoopDisposable();
    }
}