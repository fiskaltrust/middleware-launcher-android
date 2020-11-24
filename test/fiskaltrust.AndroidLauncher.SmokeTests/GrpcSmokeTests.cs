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
    [Category("grpc")]
    public class GrpcSmokeTests : AndroidLauncherSmokeTests
    {
        protected override string AppProtocol => "grpc";

        public GrpcSmokeTests(Platform platform) { }

        [Test]
        public async Task LauncherShouldStart_AndAcceptSignRequests_WhenIntentIsSent()
        {
            StartLauncher(TestConstants.Grpc.CashboxId, TestConstants.Grpc.AccessToken);
            await Task.Delay(5000);

            await WaitForStart(TestConstants.Grpc.Url, TimeSpan.FromMinutes(1));

            var serializedSignResponse = App.Invoke("SendSignTestBackdoor", new object[] { TestConstants.Grpc.Url, TestConstants.InitialOperationReceipt.Replace("{{cashbox_id}}", TestConstants.Grpc.CashboxId) }) as string;
            serializedSignResponse.Should().NotBeNullOrEmpty();

            var signResponse = JsonConvert.DeserializeObject<ReceiptResponse>(serializedSignResponse);
            signResponse.Should().NotBeNull();
            signResponse.ftState.Should().Be(0x4445000000000000);
            signResponse.ftSignatures.Should().HaveCountGreaterThan(10);
            signResponse.ftSignatures.Should().Contain(x => x.Caption == "<transaktions-nummer>");
        }
    }
}
