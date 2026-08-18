using System;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace fiskaltrust.AndroidLauncher.SmokeTests
{
    [TestFixture]
    [Category("possystemapi")]
    public class PosSystemAPIServiceSmokeTests
    {
        private const string ServiceTestActivity = "eu.fiskaltrust.androidlauncher.testclient.ServiceTestActivity";
        private const string LogTag = "ServiceTest";

        [Test]
        public async Task Echo_ShouldReturn201_WhenSentViaBoundService()
        {
            var headers = TestAppLauncher.BuildHeaders(TestConstants.Http.CashboxId, TestConstants.Http.AccessToken);
            var body = System.Text.Json.JsonSerializer.Serialize(new { Message = "Ping" });

            var statusCode = await TestAppLauncher.RunAsync(ServiceTestActivity, LogTag, "POST", "/v2/echo", headers, body, TimeSpan.FromMinutes(2));

            statusCode.Should().Be("201");
        }

        [Test]
        public async Task Sign_ShouldReturn201_WhenInitialOperationReceiptSentViaBoundService()
        {
            var headers = TestAppLauncher.BuildHeaders(TestConstants.Http.CashboxId, TestConstants.Http.AccessToken);
            var body = TestConstants.InitialOperationReceipt
                .Replace("{{cashbox_id}}", TestConstants.Http.CashboxId);

            var statusCode = await TestAppLauncher.RunAsync(ServiceTestActivity, LogTag, "POST", "/v2/sign", headers, body, TimeSpan.FromMinutes(5));

            statusCode.Should().Be("201");
        }
    }
}
