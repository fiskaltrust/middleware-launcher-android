using Microsoft.Extensions.Logging;

namespace fiskaltrust.AndroidLauncher.Common.Helpers.Logging
{
    public class FileLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => FileLogger.Instance;

        public void Dispose() { }
    }
}