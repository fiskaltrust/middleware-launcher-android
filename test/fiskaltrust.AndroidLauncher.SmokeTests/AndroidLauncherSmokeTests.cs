using fiskaltrust.Middleware.Interface.Client.Grpc;
using FluentAssertions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.XPath;

namespace fiskaltrust.AndroidLauncher.SmokeTests
{
    [TestFixture]
    public abstract class AndroidLauncherSmokeTests
    {
        protected AndroidDriver _driver;
        protected abstract string AppProtocol { get; }

        [SetUp]
        public void Init()
        {
            _driver = AppiumSetup.RunBeforeAnyTests(AppProtocol);
        }

        [TearDown]
        public void Cleanup()
        {
            AppiumSetup.RunAfterAnyTests(_driver);
        }

        private void StartActivityWithIntent(string cashboxId, string accessToken)
        {
            _driver.ExecuteScript("mobile: shell", new Dictionary<string, object> {
                { "command", "am" },
                {"args", new string[] {
                        "broadcast",
                        "-a", "android.intent.action.SEND",
                        "-n", $"eu.fiskaltrust.androidlauncher.{AppProtocol}/eu.fiskaltrust.androidlauncher.{AppProtocol}.Start",
                        "--es", "cashboxid", cashboxId,
                        "--es", "accesstoken", accessToken,
                        "--ez", "sandbox", "true"
                    }
                }
            });
        }
        protected void StartLauncher(string cashboxId, string accessToken)
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromMinutes(1));
            var elements = _driver.FindElements(MobileBy.XPath("//*"));
            TestContext.Out.WriteLine(_driver.FindElements(MobileBy.XPath("//*")));
            var element = wait.Until(wait => wait.FindElement(MobileBy.Id("ftLogo")));
            _driver.GetScreenshot();

            StartActivityWithIntent(cashboxId, accessToken);
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