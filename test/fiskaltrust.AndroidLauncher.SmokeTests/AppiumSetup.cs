using System;
using fiskaltrust.AndroidLauncher.SmokeTests.Helpers;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.Enums;

namespace fiskaltrust.AndroidLauncher.SmokeTests;

public static class AppiumSetup
{
    public static AndroidDriver RunBeforeAnyTests()
    {
        // Requires Appium installed globally: npm install -g appium && appium driver install uiautomator2
        AppiumServerHelper.StartAppiumLocalServer();

        var avdName = Environment.GetEnvironmentVariable("AVD_NAME") ?? "pixel_7_pro_-_api_34";
        var appiumUrl = Environment.GetEnvironmentVariable("APPIUM_URL") ?? "http://127.0.0.1:4723";

        var androidOptions = new AppiumOptions
        {
            AutomationName = "UIAutomator2",
            PlatformName = "Android",
        };

        androidOptions.AddAdditionalAppiumOption(MobileCapabilityType.NoReset, "true");
        androidOptions.AddAdditionalAppiumOption(AndroidMobileCapabilityType.AppPackage, "eu.fiskaltrust.androidlauncher");
        androidOptions.AddAdditionalAppiumOption(AndroidMobileCapabilityType.AppActivity, "eu.fiskaltrust.androidlauncher.MainActivity");
        androidOptions.AddAdditionalAppiumOption("avd", avdName);

        return new AndroidDriver(new Uri(appiumUrl), androidOptions, TimeSpan.FromMinutes(5));
    }

    public static void RunAfterAnyTests(AppiumDriver driver)
    {
        driver?.Quit();
        AppiumServerHelper.DisposeAppiumLocalServer();
    }
}
