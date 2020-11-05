using System;
using System.IO;
using Xamarin.UITest;

namespace fiskaltrust.AndroidLauncher.SmokeTests
{
    public class AppInitializer
    {
        public static IApp StartApp(string protocol)
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;

            path = Directory.GetParent(path).Parent.Parent.Parent.Parent.FullName;
            path = Path.Combine(path, $"src/fiskaltrust.AndroidLauncher.{protocol}/bin/Release/eu.fiskaltrust.androidlauncher.{protocol}-Signed.apk");

            return ConfigureApp
                .Android
                .EnableLocalScreenshots()
                .ApkFile(path)
                .StartApp();
        }
    }
}