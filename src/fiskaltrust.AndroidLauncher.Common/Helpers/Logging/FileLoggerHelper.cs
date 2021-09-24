using System.IO;

namespace fiskaltrust.AndroidLauncher.Common.Helpers.Logging
{
    public sealed class FileLoggerHelper
    {
        public static readonly string LogFilename = "fiskaltrust.log";
        public static readonly DirectoryInfo LogDirectory = new DirectoryInfo(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),"logs"));
    }
}