using System.Globalization;
using System.Text;
using Serilog.Sinks.File;

namespace fiskaltrust.AndroidLauncher.Helpers.Logging
{
    internal sealed class StartupHeaderHooks : FileLifecycleHooks
    {
        public override Stream OnFileOpened(string path, Stream underlyingStream, Encoding encoding)
        {
            try
            {
                var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture);
                using var writer = new StreamWriter(underlyingStream, encoding, 1024, leaveOpen: true);
                foreach (var line in StartupInfo.GetLines())
                {
                    writer.Write($"{timestamp} [INF] {line}{Environment.NewLine}");
                }
                writer.Flush();
            }
            catch (Exception ex)
            {
                Serilog.Debugging.SelfLog.WriteLine("Failed to write the log file header: {0}", ex);
            }

            return underlyingStream;
        }
    }
}
