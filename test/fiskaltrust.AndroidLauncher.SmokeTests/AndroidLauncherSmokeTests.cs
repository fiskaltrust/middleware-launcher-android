using fiskaltrust.Middleware.Interface.Client.Grpc;
using fiskaltrust.Middleware.Interface.Client.Http;
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
                        "-n", $"eu.fiskaltrust.androidlauncher/eu.fiskaltrust.androidlauncher.{AppProtocol}.Start",
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
            // var element = wait.Until(wait => wait.FindElement(MobileBy.Id("ftLogo")));
            _driver.GetScreenshot();

            StartActivityWithIntent(cashboxId, accessToken);
        }
    }
}