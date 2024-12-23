﻿using Android.App;
using Android.Content;

namespace fiskaltrust.AndroidLauncher.Grpc.Content
{
    [ContentProvider(new[] { "eu.fiskaltrust.androidlauncher.grpc.logprovider" }, Exported = false, GrantUriPermissions = true)]
    [MetaData("android.support.FILE_PROVIDER_PATHS", Resource = "@xml/log_paths")]
    public class LogContentProvider : AndroidX.Core.Content.FileProvider
    {
    }
}