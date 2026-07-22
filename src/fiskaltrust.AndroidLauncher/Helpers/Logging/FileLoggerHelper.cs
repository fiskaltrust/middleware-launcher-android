using System;
using System.Collections.Generic;
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
            return Directory.Exists(LogDirectory.FullName) ? LogDirectory.GetFiles("*.log") : Array.Empty<FileInfo>();
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

        public static List<string> SplitIntoLines(string content)
        {
            if (string.IsNullOrEmpty(content)) return new List<string>();

            var rawLines = content.Split('\n');
            var result = new List<string>(rawLines.Length);
            for (int i = 0; i < rawLines.Length; i++)
            {
                if (i == rawLines.Length - 1 && rawLines[i].Length == 0) continue;
                result.Add(rawLines[i].TrimEnd('\r'));
            }
            return result;
        }

        public static List<string> ReadNewLines(FileInfo logFile, ref long offset)
        {
            using var fs = logFile.OpenRead();
            if (offset > fs.Length) offset = 0;
            fs.Seek(offset, SeekOrigin.Begin);

            using var sr = new StreamReader(fs);
            var content = sr.ReadToEnd();
            offset = fs.Length;

            return SplitIntoLines(content);
        }
    }
}
