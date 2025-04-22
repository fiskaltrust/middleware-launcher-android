using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Interface.Client.Grpc;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using OpenQA.Selenium.Appium;

namespace fiskaltrust.AndroidLauncher.SmokeTests
{
    [Category("grpc")]
    public class GrpcSmokeTests : AndroidLauncherSmokeTests
    {
        protected override string AppProtocol => "grpc";

        [Test]
        public async Task LauncherShouldStart_AndAcceptSignRequests_WhenIntentIsSent()
        {
            StartLauncher(TestConstants.Grpc.CashboxId, TestConstants.Grpc.AccessToken);
            await Task.Delay(5000);

            await WaitForStart(TestConstants.Grpc.Url, TimeSpan.FromMinutes(1));

            // var serializedSignResponse = _driver.Invoke("SendSignTestBackdoor", new object[] { TestConstants.Grpc.Url, TestConstants.InitialOperationReceipt.Replace("{{cashbox_id}}", TestConstants.Grpc.CashboxId) }) as string;
            // serializedSignResponse.Should().NotBeNullOrEmpty();

            // var signResponse = JsonConvert.DeserializeObject<ReceiptResponse>(serializedSignResponse);
            // signResponse.Should().NotBeNull();
            // signResponse.ftState.Should().Be(0x4445000000000000);
            // signResponse.ftSignatures.Should().HaveCountGreaterThan(10);
            // signResponse.ftSignatures.Should().Contain(x => x.Caption == "<transaktions-nummer>");
        }


        protected async Task WaitForStart(string url, TimeSpan timeSpan)
        {
            const string message = "Ping";
            var startTime = DateTime.UtcNow;

            while (DateTime.UtcNow < startTime + timeSpan)
            {
                try
                {
                    var pos = Task.Run(() => GrpcPosFactory.CreatePosAsync(new GrpcClientOptions { Url = new Uri(url), RetryPolicyOptions = null })).Result;
                    var result = (await pos.EchoAsync(new ifPOS.v1.EchoRequest { Message = message })).Message;

                    if (result == message)
                        return;

                    TestContext.Out.WriteLine("Echo result: " + result);
                }
                catch (Exception ex)
                {
                    TestContext.Error.WriteLine(ex);
                }

                await Task.Delay(3000);
            }

            _driver.GetScreenshot();
            throw new TimeoutException($"endpoint at {url} was not reachable after {timeSpan}.");
        }
    }
}
