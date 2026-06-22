using NUnit.Framework;
using OpenQA.Selenium.Appium.Android;

namespace fiskaltrust.AndroidLauncher.SmokeTests
{
    [TestFixture]
    public abstract class AndroidLauncherSmokeTests
    {
        protected AndroidDriver _driver;

        [OneTimeSetUp]
        public void InitFixture()
        {
            AppiumSetup.Setup();
            _driver = AppiumSetup.RunBeforeAnyTests();
        }

        [OneTimeTearDown]
        public void CleanupFixture()
        {
            AppiumSetup.RunAfterAnyTests(_driver);
        }
    }
}
