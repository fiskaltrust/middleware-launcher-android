using Android.OS;
using Serilog;

namespace fiskaltrust.AndroidLauncher.Helpers.Logging
{
    internal static class StartupInfo
    {
        public static Guid? CashboxId { get; set; }
        public static bool? IsSandbox { get; set; }

        public static IEnumerable<string> GetLines()
        {
            yield return Format("fiskaltrust.AndroidLauncher:", $"{AppInfo.Current.VersionString} (Build {AppInfo.Current.BuildString})");
            yield return Format("Android:", $"{Build.VERSION.Release} (API {(int)Build.VERSION.SdkInt})");
            yield return Format("Device:", $"{Build.Manufacturer} {Build.Model}");
            if (CashboxId is not null)
            {
                yield return CashboxIdLine();
            }
            if (IsSandbox is not null)
            {
                yield return SandboxLine();
            }
        }

        public static void LogCashboxId() => Serilog.Log.Logger.Information("{StartupInfo:l}", CashboxIdLine());

        public static void LogSandbox() => Serilog.Log.Logger.Information("{StartupInfo:l}", SandboxLine());

        private static string CashboxIdLine() => Format("CashBoxId:", CashboxId?.ToString() ?? "-");

        private static string SandboxLine() => Format("Sandbox:", IsSandbox?.ToString() ?? "-");

        private static string Format(string label, string value) => $"{label} {value}";
    }
}
