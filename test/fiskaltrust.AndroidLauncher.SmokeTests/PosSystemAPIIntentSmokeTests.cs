using System;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace fiskaltrust.AndroidLauncher.SmokeTests
{
    [TestFixture]
    [Category("possystemapi")]
    public class PosSystemAPIIntentSmokeTests
    {
        private const string ActivityTestActivity = "eu.fiskaltrust.androidlauncher.testclient.ActivityTestActivity";
        private const string LogTag = "ActivityTest";

        [Test]
        public async Task Echo_ShouldReturn201_WhenSentViaPosSystemAPIIntent()
        {
            var headers = TestAppLauncher.BuildHeaders(TestConstants.Http.CashboxId, TestConstants.Http.AccessToken);
            var body = System.Text.Json.JsonSerializer.Serialize(new { Message = "Ping" });

            var statusCode = await TestAppLauncher.RunAsync(ActivityTestActivity, LogTag, "POST", "/v2/echo", headers, body, TimeSpan.FromMinutes(2));

            statusCode.Should().Be("201");
        }

        [Test]
        public async Task Sign_ShouldReturn201_WhenInitialOperationReceiptSentViaPosSystemAPIIntent()
        {
            var headers = TestAppLauncher.BuildHeaders(TestConstants.Http.CashboxId, TestConstants.Http.AccessToken);
            var body = TestConstants.InitialOperationReceipt
                .Replace("{{cashbox_id}}", TestConstants.Http.CashboxId);

            var statusCode = await TestAppLauncher.RunAsync(ActivityTestActivity, LogTag, "POST", "/v2/sign", headers, body, TimeSpan.FromMinutes(5));

            statusCode.Should().Be("201");
        }
    }
}
