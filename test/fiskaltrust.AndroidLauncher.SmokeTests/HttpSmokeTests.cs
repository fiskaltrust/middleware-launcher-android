using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using Xamarin.UITest;

namespace fiskaltrust.AndroidLauncher.SmokeTests
{
    [TestFixture(Platform.Android)]
    [Category("http")]
    public class HttpSmokeTests : AndroidLauncherSmokeTests
    {
        protected override string AppProtocol => "http";

        public HttpSmokeTests(Platform platform) { }

        [Test]
        public async Task LauncherShouldStart_AndAcceptSignRequests_WhenIntentIsSent()
        {
            StartLauncher(TestConstants.Http.CashboxId, TestConstants.Http.AccessToken);
            await Task.Delay(3000);

            await WaitForStart(TestConstants.Http.Url, TimeSpan.FromMinutes(1));

            var serializedSignResponse = App.Invoke("SendSignTestBackdoor", new object[] { TestConstants.Http.Url, TestConstants.InitialOperationReceipt.Replace("{{cashbox_id}}", TestConstants.Http.CashboxId)}) as string;
            serializedSignResponse.Should().NotBeNullOrEmpty();

            var signResponse = JsonConvert.DeserializeObject<ReceiptResponse>(serializedSignResponse);
            signResponse.Should().NotBeNull();
            signResponse.ftState.Should().Be(0x4445000000000000);
            signResponse.ftSignatures.Should().HaveCountGreaterThan(10);
            signResponse.ftSignatures.Should().Contain(x => x.Caption == "<transaktions-nummer>");
        }
    }
}
