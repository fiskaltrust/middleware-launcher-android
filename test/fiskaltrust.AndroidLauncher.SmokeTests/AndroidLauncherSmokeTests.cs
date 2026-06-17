using NUnit.Framework;
using OpenQA.Selenium.Appium.Android;

namespace fiskaltrust.AndroidLauncher.SmokeTests
{
    [TestFixture]
    public abstract class AndroidLauncherSmokeTests
    {
        protected AndroidDriver _driver;

        [SetUp]
        public void Init()
        {
            _driver = AppiumSetup.RunBeforeAnyTests();
        }

        [TearDown]
        public void Cleanup()
        {
            AppiumSetup.RunAfterAnyTests(_driver);
        }
    }
}
