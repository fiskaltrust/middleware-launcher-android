using FluentAssertions;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Xamarin.UITest;

namespace fiskaltrust.AndroidLauncher.SmokeTests
{
    public abstract class AndroidLauncherSmokeTests
    {
        protected IApp App;

        protected abstract string AppProtocol { get; }

        [SetUp]
        public void BeforeEachTest()
        {
            App = AppInitializer.StartApp(AppProtocol);
        }

        protected void StartLauncher(string cashboxId, string accessToken)
        {
            var results = App.WaitForElement(c => c.Id("ftLogo"));
            App.Screenshot("MainActivity");
            results.Should().HaveCount(1);

            App.Invoke("SendStartIntentTestBackdoor", new object[] { cashboxId, accessToken });
        }

        protected async Task WaitForStart(string url, TimeSpan timeSpan)
        {
            const string message = "Ping";
            var startTime = DateTime.UtcNow;

            while (DateTime.UtcNow < startTime + timeSpan)
            {
                try
                {
                    var result = App.Invoke("SendEchoTestBackdoor", new object[] { url, message }) as string;
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

            App.Screenshot("Timeout");
            throw new TimeoutException($"endpoint at {url} was not reachable after {timeSpan}.");
        }
    }
}