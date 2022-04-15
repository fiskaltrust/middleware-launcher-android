using Android.App;
using Android.Content;

namespace fiskaltrust.AndroidLauncher.Http.Content
{
    [ContentProvider(new[] { "eu.fiskaltrust.androidlauncher.http.logprovider" }, Exported = false, GrantUriPermissions = true)]
    [MetaData("android.support.FILE_PROVIDER_PATHS", Resource = "@xml/log_paths")]
    public class LogContentProvider : AndroidX.Core.Content.FileProvider
    {
    }
}