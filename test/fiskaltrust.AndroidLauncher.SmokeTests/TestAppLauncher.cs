using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;

namespace fiskaltrust.AndroidLauncher.SmokeTests
{
    internal static class TestAppLauncher
    {
        private const string TestAppPackage = "eu.fiskaltrust.androidlauncher.testclient";

        public static async Task<string> RunAsync(
            string activityClass,
            string logTag,
            string method,
            string path,
            Dictionary<string, string> headers,
            string body,
            TimeSpan timeout)
        {
            var headersBase64Url = ToBase64Url(System.Text.Json.JsonSerializer.Serialize(headers));
            var bodyBase64Url = ToBase64Url(body);

            RunAdb("logcat -c");

            using var logcat = Process.Start(new ProcessStartInfo("adb", "logcat")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
            })!;

            try
            {
                TestContext.Out.WriteLine($"Starting {activityClass}: {method} {path}");
                RunAdb($"shell am start -n {TestAppPackage}/{activityClass} " +
                       $"--es Method {method} --es Path {path} " +
                       $"--es HeaderJsonObjectBase64Url {headersBase64Url} " +
                       $"--es BodyBase64Url {bodyBase64Url}");

                var deadline = DateTime.UtcNow + timeout;
                while (DateTime.UtcNow < deadline)
                {
                    var readLineTask = logcat.StandardOutput.ReadLineAsync();
                    var remaining = deadline - DateTime.UtcNow;
                    if (await Task.WhenAny(readLineTask, Task.Delay(remaining)) != readLineTask)
                        break;

                    var line = await readLineTask;
                    if (line == null)
                        break;

                    var isRelevant = line.Contains("PosSystemAPI:") || line.Contains("PosSystemAPIService:") || line.Contains($"{logTag}:");
                    if (!isRelevant)
                        continue;

                    TestContext.Out.WriteLine($"[LOGCAT] {line.Trim()}");

                    if (line.Contains($"{logTag}:") && line.Contains("RESULT="))
                    {
                        var match = Regex.Match(line, @"RESULT=(\S+)");
                        if (match.Success)
                            return match.Groups[1].Value;
                    }
                }
            }
            finally
            {
                try { logcat.Kill(); } catch { }
            }

            throw new TimeoutException($"{activityClass} did not report a RESULT for {method} {path} within {timeout}");
        }

        public static Dictionary<string, string> BuildHeaders(string cashboxId, string accessToken) =>
            new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "x-cashbox-id", cashboxId },
                { "x-cashbox-accesstoken", accessToken },
                { "x-operation-id", Guid.NewGuid().ToString() }
            };

        private static string ToBase64Url(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string RunAdb(string arguments)
        {
            var psi = new ProcessStartInfo("adb", arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using var process = Process.Start(psi)!;
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }
    }
}
