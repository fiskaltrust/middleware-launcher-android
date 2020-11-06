using Android.App;
using Android.Content;
using fiskaltrust.AndroidLauncher.Common;
using fiskaltrust.AndroidLauncher.Common.Bootstrapping;
using fiskaltrust.AndroidLauncher.Common.Constants;
using fiskaltrust.AndroidLauncher.Common.Helpers;
using fiskaltrust.AndroidLauncher.Http.Helpers;
using fiskaltrust.AndroidLauncher.Http.Hosting;

namespace fiskaltrust.AndroidLauncher.Http.Broadcasting
{
    [BroadcastReceiver(Enabled = true, Exported = true, Name = BroadcastConstants.HttpStartBroadcastName)]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault })]
    public class StartLauncherBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            ServiceLocator.Register<IHostFactory>(new HttpHostFactory());
            ServiceLocator.Register<IUrlResolver>(new RestUrlResolver());

            LauncherBootstrapper.Setup(context, intent);
        }
    }
}
