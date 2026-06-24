using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using NUnit.Framework;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.Enums;

namespace fiskaltrust.AndroidLauncher.SmokeTests;

public static class AppiumSetup
{
    public static AndroidDriver RunBeforeAnyTests()
    {
        var androidOptions = new AppiumOptions
        {
            AutomationName = "UIAutomator2",
            PlatformName = "Android",
        };

        androidOptions.AddAdditionalAppiumOption(MobileCapabilityType.NoReset, "true");
        androidOptions.AddAdditionalAppiumOption(AndroidMobileCapabilityType.AppPackage, "eu.fiskaltrust.androidlauncher");
        androidOptions.AddAdditionalAppiumOption(AndroidMobileCapabilityType.AppActivity, "eu.fiskaltrust.androidlauncher.MainActivity");

        return new AndroidDriver(androidOptions);
    }

    public static void RunAfterAnyTests(AndroidDriver driver)
    {
        driver?.Quit();
    }
}
