using System.Collections.Generic;
using OpenQA.Selenium.Appium.Service;
using OpenQA.Selenium.Appium.Service.Options;

namespace fiskaltrust.AndroidLauncher.SmokeTests.Helpers;

public static class AppiumServerHelper
{
    private static AppiumLocalService? _appiumLocalService;

    public const string DefaultHostAddress = "127.0.0.1";
    public const int DefaultHostPort = 4723;

    public static void StartAppiumLocalServer(string host = DefaultHostAddress, int port = DefaultHostPort)
    {
        if (_appiumLocalService is not null)
            return;

        var args = new OptionCollector().AddArguments(new KeyValuePair<string, string>("--allow-insecure", "adb_shell"));

        _appiumLocalService = new AppiumServiceBuilder()
            .WithIPAddress(host)
            .UsingPort(port)
            .WithArguments(args)
            .Build();

        _appiumLocalService.Start();
    }

    public static void DisposeAppiumLocalServer()
    {
        _appiumLocalService?.Dispose();
    }
}
