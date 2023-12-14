using Android.App;
using Android.Content;
using Android.OS;
using fiskaltrust.AndroidLauncher.Common.AndroidService;
using fiskaltrust.AndroidLauncher.Common.Constants;
using fiskaltrust.AndroidLauncher.Common.Extensions;
using fiskaltrust.AndroidLauncher.Common.Helpers;
using fiskaltrust.AndroidLauncher.Common.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Common.Broadcasting
{
    [BroadcastReceiver(Enabled = true, Exported = true, Name = BroadcastConstants.RequestStoragePermissionName)]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault })]
    public class RequestStoragePermissionBroadcastReceiver : BroadcastReceiver
    {
        private readonly Func<string, string> _getStoragePermisson;
        public RequestStoragePermissionBroadcastReceiver(Func<string, string> getStoragePermisson)
        {
            _getStoragePermisson = getStoragePermisson;
        }

        public override void OnReceive(Context context, Intent intent)
        {
            var directory = intent.GetStringExtra("directory");
            var id = intent.GetStringExtra("id");

            var uri = _getStoragePermisson(directory);

            var response = new Intent(BroadcastConstants.GrantStoragePermissionName);
            response.PutExtra("uri", uri);
            response.PutExtra("id", id);
            intent.SetPackage(Application.Context.PackageName);
            Application.Context.SendBroadcast(response);
        }
    }

    [BroadcastReceiver(Enabled = true, Exported = true, Name = BroadcastConstants.GrantStoragePermissionName)]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault })]
    public class GrantStoragePermissionBroadcastReceiver : BroadcastReceiver
    {
        private readonly TaskCompletionSource<string> _callback;
        private readonly Guid _id;

        public GrantStoragePermissionBroadcastReceiver(TaskCompletionSource<string> callback, Guid id)
        {
            _callback = callback;
            _id = id;
        }

        public override void OnReceive(Context context, Intent intent)
        {
            var uri = intent.GetStringExtra("uri");
            var id = intent.GetStringExtra("id");
            if(id == _id.ToString())
            {
                _callback.TrySetResult(uri);
            }
        }
    }
}
