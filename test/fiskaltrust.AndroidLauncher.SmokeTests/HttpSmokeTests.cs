using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Interface.Client.Http;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using OpenQA.Selenium.Appium;

namespace fiskaltrust.AndroidLauncher.SmokeTests
{
    [Category("http")]
    public class HttpSmokeTests : AndroidLauncherSmokeTests
    {
        protected override string AppProtocol => "http";

        [Test]
        public async Task LauncherShouldStart_AndAcceptSignRequests_WhenIntentIsSent()
        {
            StartLauncher(TestConstants.Http.CashboxId, TestConstants.Http.AccessToken);
            await Task.Delay(5000);

            await WaitForStart(TestConstants.Http.Url, TimeSpan.FromMinutes(1));
        }


        protected async Task WaitForStart(string url, TimeSpan timeSpan)
        {
            const string message = "Ping";
            var startTime = DateTime.UtcNow;

            while (DateTime.UtcNow < startTime + timeSpan)
            {
                try
                {
                    var pos = Task.Run(() => HttpPosFactory.CreatePosAsync(new HttpPosClientOptions { Url = new Uri(url), RetryPolicyOptions = null })).Result;
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
