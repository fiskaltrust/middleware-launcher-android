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
            using FileStream fs = logFile.OpenRead();
            var end = fs.Length;
            var buffer = new byte[8192];
            var position = end;
            var start = 0L;
            var count = 0;

            while (position > 0 && count < lineCount)
            {
                var size = (int)Math.Min(buffer.Length, position);
                position -= size;
                fs.Seek(position, SeekOrigin.Begin);
                fs.ReadExactly(buffer, 0, size);

                for (var i = size - 1; i >= 0; i--)
                {
                    if (buffer[i] != '\n' || position + i == end - 1) continue;
                    if (++count < lineCount) continue;
                    start = position + i + 1;
                    break;
                }
            }

            fs.Seek(start, SeekOrigin.Begin);
            using var sr = new StreamReader(fs);
            var lines = sr.ReadToEnd();
            return lines;
        }

        public static int CountLines(FileInfo logFile)
        {
            using var fs = logFile.OpenRead();
            if (fs.Length == 0) return 0;

            var buffer = new byte[8192];
            var count = 0;
            byte lastByte = 0;
            int bytesRead;
            while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (var i = 0; i < bytesRead; i++)
                {
                    if (buffer[i] == (byte)'\n') count++;
                }
                lastByte = buffer[bytesRead - 1];
            }

            if (lastByte != (byte)'\n') count++;
            return count;
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
