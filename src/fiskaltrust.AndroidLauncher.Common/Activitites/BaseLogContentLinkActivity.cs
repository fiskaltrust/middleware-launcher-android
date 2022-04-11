using Android.App;
using Android.Content;
using fiskaltrust.AndroidLauncher.Common.Helpers.Logging;
using fiskaltrust.AndroidLauncher.Common.Services.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Common.Activitites
{
    public abstract class BaseLogContentLinkActivity : Activity
    {
        protected override async void OnStart()
        {
            base.OnStart();
            var cashboxId = Intent.GetStringExtra("cashboxid");
            var accessToken = Intent.GetStringExtra("accesstoken");

            var latestLogPath = FileLoggerHelper.LogDirectory.GetFiles("*.log").OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
            if (await AuthenticateAsync(cashboxId, accessToken) && latestLogPath != null)
            {
                var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(ApplicationContext, "eu.fiskaltrust.androidlauncher.http.logprovider", new Java.IO.File(latestLogPath.FullName));
                var result = new Intent().SetData(uri).SetFlags(ActivityFlags.GrantReadUriPermission);
                SetResult(Result.Ok, result);
            }
            else
            {
                SetResult(Result.Canceled);
            }

            Finish();
        }

        private async Task<bool> AuthenticateAsync(string cashboxid, string accesstoken)
        {
            var configProvider = new LocalConfigurationProvider();
            return await configProvider.IsConfigStoreEmptyAsync() || !Guid.TryParse(cashboxid, out var id) || await configProvider.ConfigurationExistsAsync(id, accesstoken);
        }
    }
}