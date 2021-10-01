using System;
using System.IO;
using System.Linq;

namespace fiskaltrust.AndroidLauncher.Common.Helpers.Logging
{
    public sealed class FileLoggerHelper
    {
        public static readonly string LogFilename = "fiskaltrust.log";
        public static readonly DirectoryInfo LogDirectory = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "logs"));

        public static string GetLastLinesOfCurrentLogFile(int lineCount)
        {
            var currentLogFile = LogDirectory.GetFiles("*.log").OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
            if (currentLogFile == null) return "";

            int count = 0;
            byte[] buffer = new byte[1];

            using FileStream fs = currentLogFile.OpenRead();
            fs.Seek(0, SeekOrigin.End);

            while (count < lineCount)
            {
                try
                {
                    fs.Seek(-1, SeekOrigin.Current);
                    fs.Read(buffer, 0, 1);
                    if (buffer[0] == '\n')
                    {
                        count++;
                    }

                    fs.Seek(-1, SeekOrigin.Current);
                }
                catch
                {
                    break;
                }
            }
            fs.Seek(1, SeekOrigin.Current);

            using var sr = new StreamReader(fs);
            var lines = sr.ReadToEnd();
            return lines;
        }
    }
}