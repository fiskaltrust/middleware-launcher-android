using System;
using System.IO;
using System.Linq;

namespace fiskaltrust.AndroidLauncher.Helpers.Logging
{
    public sealed class FileLoggerHelper
    {
        public static readonly string LogFilename = "fiskaltrust.log";
        public static readonly DirectoryInfo LogDirectory = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "logs"));

        public static FileInfo[] GetLogFiles()
        {
            return LogDirectory.Exists ? LogDirectory.GetFiles("*.log") : Array.Empty<FileInfo>();
        }

        public static List<FileInfo> GetLogFilesOrderedByDateDescending()
        {
            return GetLogFiles().OrderByDescending(f => f.LastWriteTime).ToList();
        }

        public static string GetLastLines(FileInfo logFile, int lineCount)
        {
            int count = 0;
            byte[] buffer = new byte[1];

            using FileStream fs = logFile.OpenRead();
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
