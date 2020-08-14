using System;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.AndroidLauncher.Common.Helpers.Logging
{
    public class AndroidLogger : ILogger
    {
        private const string TAG = "fiskaltrust.AndroidLauncher";

        public IDisposable BeginScope<TState>(TState state) => new NoopDisposable();

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            switch (logLevel)
            {
                case LogLevel.None:
                case LogLevel.Trace:
                case LogLevel.Debug:
                    Android.Util.Log.Debug(TAG, formatter(state, exception));
                    break;
                case LogLevel.Information:
                    Android.Util.Log.Info(TAG, formatter(state, exception));
                    break;
                case LogLevel.Warning:
                    Android.Util.Log.Warn(TAG, formatter(state, exception));
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    Android.Util.Log.Error(TAG, formatter(state, exception));
                    break;
            }
        }

        private class NoopDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}