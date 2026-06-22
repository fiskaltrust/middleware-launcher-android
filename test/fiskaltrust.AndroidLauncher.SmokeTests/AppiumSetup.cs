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
    private static string _deviceUdid;

    public static void Setup()
    {
        var appiumUrl = Environment.GetEnvironmentVariable("APPIUM_URL") ?? "http://127.0.0.1:4723";
        CheckAppium(appiumUrl);

        _deviceUdid = Environment.GetEnvironmentVariable("DEVICE_UDID") ?? DetectDevice();
        TestContext.Out.WriteLine($"Using device: {_deviceUdid}");

        var repoRoot = GetRepoRoot();
        var (apkPath, wasBuilt) = EnsureApk(repoRoot);
        if (wasBuilt)
            InstallApk(apkPath, _deviceUdid);
    }

    public static AndroidDriver RunBeforeAnyTests()
    {
        var appiumUrl = Environment.GetEnvironmentVariable("APPIUM_URL") ?? "http://127.0.0.1:4723";

        var androidOptions = new AppiumOptions
        {
            AutomationName = "UIAutomator2",
            PlatformName = "Android",
        };

        androidOptions.AddAdditionalAppiumOption(MobileCapabilityType.NoReset, "true");
        androidOptions.AddAdditionalAppiumOption(AndroidMobileCapabilityType.AppPackage, "eu.fiskaltrust.androidlauncher");
        androidOptions.AddAdditionalAppiumOption(AndroidMobileCapabilityType.AppActivity, "eu.fiskaltrust.androidlauncher.MainActivity");

        if (_deviceUdid != null)
            androidOptions.AddAdditionalAppiumOption(MobileCapabilityType.Udid, _deviceUdid);

        return new AndroidDriver(new Uri(appiumUrl), androidOptions, TimeSpan.FromMinutes(5));
    }

    public static void RunAfterAnyTests(AppiumDriver driver)
    {
        driver?.Quit();
    }

    private static void CheckAppium(string appiumUrl)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            client.GetAsync($"{appiumUrl}/status").GetAwaiter().GetResult().EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Appium is not running at {appiumUrl}. Start Appium first.", ex);
        }
    }

    private static string DetectDevice()
    {
        var output = RunProcess("adb", "devices");
        var devices = output
            .Split('\n')
            .Skip(1)
            .Where(line => line.Contains('\t') && line.TrimEnd().EndsWith("device"))
            .Select(line => line.Split('\t')[0].Trim())
            .ToList();

        if (devices.Count == 0)
            throw new InvalidOperationException("No Android device or emulator connected. Connect a device or start an emulator.");

        if (devices.Count > 1)
            TestContext.Out.WriteLine($"Multiple devices detected, using: {devices[0]}. Set DEVICE_UDID env var to override.");

        return devices[0];
    }

    private static (string apkPath, bool wasBuilt) EnsureApk(string repoRoot)
    {
        var rid = Environment.GetEnvironmentVariable("ANDROID_RID") ?? "android-x64";
        var outputDir = Path.Combine(repoRoot, "src", "fiskaltrust.AndroidLauncher", "bin", "Debug", "net9.0-android", rid);
        var srcDir = Path.Combine(repoRoot, "src", "fiskaltrust.AndroidLauncher");

        var existingApk = Directory.Exists(outputDir)
            ? Directory.GetFiles(outputDir, "*-Signed.apk", SearchOption.AllDirectories).FirstOrDefault()
            : null;

        if (existingApk == null)
        {
            TestContext.Out.WriteLine("APK not found, building...");
        }
        else
        {
            var apkTime = File.GetLastWriteTimeUtc(existingApk);
            var sep = Path.DirectorySeparatorChar;
            var hasChanges = Directory.GetFiles(srcDir, "*.*", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{sep}bin{sep}") && !f.Contains($"{sep}obj{sep}"))
                .Any(f => File.GetLastWriteTimeUtc(f) > apkTime);

            if (!hasChanges)
            {
                TestContext.Out.WriteLine("APK is up to date, skipping build.");
                return (existingApk, false);
            }

            TestContext.Out.WriteLine("Source files changed, rebuilding APK...");
        }

        var projectPath = Path.Combine(repoRoot, "src", "fiskaltrust.AndroidLauncher", "fiskaltrust.AndroidLauncher.csproj");
        var buildArgs = $"publish \"{projectPath}\" -f net9.0-android -p:NuGetAudit=false -p:AndroidPackageFormat=apk -p:RuntimeIdentifier={rid}";
        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME")?.Split(';')[0].Trim();
        if (!string.IsNullOrEmpty(javaHome))
            buildArgs += $" -p:JavaSdkDirectory=\"{javaHome}\"";

        RunProcess("dotnet", buildArgs, timeoutMinutes: 15);

        var apkPath = Directory.GetFiles(outputDir, "*-Signed.apk", SearchOption.AllDirectories).FirstOrDefault()
            ?? throw new FileNotFoundException($"APK not found in {outputDir} after build.");
        return (apkPath, true);
    }

    private static void InstallApk(string apkPath, string deviceUdid)
    {
        TestContext.Out.WriteLine($"Installing APK on {deviceUdid}...");
        RunProcess("adb", $"-s {deviceUdid} install -r \"{apkPath}\"");
    }

    private static string GetRepoRoot()
    {
        // DLL sits at test\fiskaltrust.AndroidLauncher.SmokeTests\bin\Debug\net9.0\
        return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
    }

    private static string RunProcess(string fileName, string arguments, int timeoutMinutes = 2)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(psi);
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        process.WaitForExit((int)TimeSpan.FromMinutes(timeoutMinutes).TotalMilliseconds);

        var output = outputTask.GetAwaiter().GetResult();
        var error = errorTask.GetAwaiter().GetResult();

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"'{fileName} {arguments}' failed (exit {process.ExitCode}):\n{error}");

        return output;
    }
}
