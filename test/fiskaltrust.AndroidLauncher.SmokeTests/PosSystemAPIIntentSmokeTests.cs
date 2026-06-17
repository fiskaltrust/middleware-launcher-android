using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace fiskaltrust.AndroidLauncher.SmokeTests
{
    [TestFixture]
    [Category("possystemapi")]
    public class PosSystemAPIIntentSmokeTests : AndroidLauncherSmokeTests
    {
        [Test]
        public async Task Echo_ShouldReturn200_WhenSentViaPosSystemAPIIntent()
        {
            var headers = BuildHeaders(TestConstants.Http.CashboxId, TestConstants.Http.AccessToken);
            var body = System.Text.Json.JsonSerializer.Serialize(new { Message = "Ping" });

            // First call may trigger middleware startup, hence the longer timeout
            var result = await SendPosSystemAPIIntentAsync("POST", "/v2/echo", headers, body, TimeSpan.FromMinutes(2));

            result.StatusCode.Should().Be("201");
        }

        [Test]
        public async Task Sign_ShouldReturn200_WhenInitialOperationReceiptSentViaPosSystemAPIIntent()
        {
            var headers = BuildHeaders(TestConstants.Http.CashboxId, TestConstants.Http.AccessToken);
            var body = TestConstants.InitialOperationReceipt
                .Replace("{{cashbox_id}}", TestConstants.Http.CashboxId);

            var result = await SendPosSystemAPIIntentAsync("POST", "/v2/sign", headers, body, TimeSpan.FromMinutes(3));

            result.StatusCode.Should().Be("201");
        }

        private static Dictionary<string, string> BuildHeaders(string cashboxId, string accessToken) =>
            new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "x-cashbox-id", cashboxId },
                { "x-cashbox-accesstoken", accessToken },
                { "x-operation-id", Guid.NewGuid().ToString() }
            };

        private async Task<IntentCallResult> SendPosSystemAPIIntentAsync(
            string method,
            string path,
            Dictionary<string, string> headers,
            string body = null,
            TimeSpan? timeout = null)
        {
            var timeoutValue = timeout ?? TimeSpan.FromSeconds(30);

            var headersBase64Url = ToBase64Url(System.Text.Json.JsonSerializer.Serialize(headers));

            // Clear logcat so we only capture logs from this request
            _driver.ExecuteScript("mobile: shell", new Dictionary<string, object>
            {
                { "command", "logcat" },
                { "args", new[] { "-c" } }
            });

            var args = new List<string>
            {
                "start",
                "-n", "eu.fiskaltrust.androidlauncher/eu.fiskaltrust.androidlauncher.PosSystemAPI",
                "--es", "Method", method,
                "--es", "Path", path,
                "--es", "HeaderJsonObjectBase64Url", headersBase64Url
            };

            if (!string.IsNullOrEmpty(body))
                args.AddRange(new[] { "--es", "BodyBase64Url", ToBase64Url(body) });

            TestContext.Out.WriteLine($"Sending PosSystemAPI Intent: {method} {path}");

            _driver.ExecuteScript("mobile: shell", new Dictionary<string, object>
            {
                { "command", "am" },
                { "args", args.ToArray() }
            });

            // Poll logcat for the activity's response log line
            var startTime = DateTime.UtcNow;
            while (DateTime.UtcNow < startTime + timeoutValue)
            {
                await Task.Delay(1000);

                var logs = _driver.Manage().Logs.GetLog("logcat");
                foreach (var log in logs)
                {
                    var message = log.Message?.ToString() ?? "";
                    if (message.Contains("PosSystemAPI") || message.Contains("fiskaltrust.AndroidLauncher"))
                    {
                        TestContext.Out.WriteLine($"[LOGCAT] {message}");
                    }
                    if (message.Contains("PosSystemAPI") && message.Contains("Finishing with response:"))
                    {
                        var match = Regex.Match(message, @"Finishing with response: (\d+)");
                        if (match.Success)
                        {
                            var statusCode = match.Groups[1].Value;
                            TestContext.Out.WriteLine($"Got response: {statusCode}");
                            return new IntentCallResult { StatusCode = statusCode };
                        }
                    }
                }
            }

            _driver.GetScreenshot();
            throw new TimeoutException($"PosSystemAPI did not respond to {method} {path} within {timeoutValue}");
        }

        private static string ToBase64Url(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private class IntentCallResult
        {
            public string StatusCode { get; set; }
        }
    }
}
