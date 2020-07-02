using Microsoft.Extensions.Logging;

namespace fiskaltrust.AndroidLauncher.Helpers
{
    public class AndroidLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new AndroidLogger();

        public void Dispose() { }
    }
}