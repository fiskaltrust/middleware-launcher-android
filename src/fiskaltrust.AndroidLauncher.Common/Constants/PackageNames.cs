using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using fiskaltrust.AndroidLauncher.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fiskaltrust.AndroidLauncher.Common.Constants
{
    public class PackageNames
    {
        private static readonly Dictionary<LauncherType, string> _values = new Dictionary<LauncherType, string>()
        {
            { LauncherType.Http, "eu.fiskaltrust.androidlauncher.http"},
            { LauncherType.Grpc, "eu.fiskaltrust.androidlauncher.grpc"},
        };

        public static string Get(LauncherType type) { return _values[type]; }
    }
}
