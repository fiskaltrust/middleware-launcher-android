using Android.App;
using Android.Content;
using fiskaltrust.AndroidLauncher.Common;
using fiskaltrust.AndroidLauncher.Common.Bootstrapping;
using fiskaltrust.AndroidLauncher.Common.Constants;
using fiskaltrust.AndroidLauncher.Common.Helpers;
using fiskaltrust.AndroidLauncher.Grpc.Hosting;
using fiskaltrust.AndroidLauncher.Helpers;

namespace fiskaltrust.AndroidLauncher.Grpc.Broadcasting
{
    [BroadcastReceiver(Enabled = true, Exported = true, Name = BroadcastConstants.GrpcStartBroadcastName)]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault })]
    public class StartLauncherBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            ServiceLocator.Register<IHostFactory>(new GrpcHostFactory());
            ServiceLocator.Register<IUrlResolver>(new GrpcUrlResolver());

            LauncherBootstrapper.Setup(context, intent);
        }
    }
}
