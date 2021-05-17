using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Java.Interop;
using Android.Content;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Interface.Client.Grpc;
using System;
using Newtonsoft.Json;
using fiskaltrust.ifPOS.v1;
using Android.Widget;
using Xamarin.Essentials;
using Android.Util;
using fiskaltrust.AndroidLauncher.Common.Helpers.Logging;

namespace fiskaltrust.AndroidLauncher.Grpc
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            VersionTracking.Track();

            SetContentView(Common.Resource.Layout.activity_main);

            Init();
        }

        private void Init()
        {
            FindViewById<TextView>(Common.Resource.Id.textViewVersion).Text = $"Version {VersionTracking.CurrentVersion}";
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        [Export("SendStartIntentTestBackdoor")]
        public void SendStartIntentTestBackdoor(string cashboxid, string accesstoken)
        {
            var componentName = new ComponentName("eu.fiskaltrust.androidlauncher.grpc", "eu.fiskaltrust.androidlauncher.grpc.Start");

            var intent = new Intent(Intent.ActionSend);
            intent.SetComponent(componentName);
            intent.PutExtra("cashboxid", cashboxid);
            intent.PutExtra("accesstoken", accesstoken);
            intent.PutExtra("sandbox", true);

            SendBroadcast(intent);
        }

        [Export("SendEchoTestBackdoor")]
        public string SendEchoTestBackdoor(string url, string message)
        {
            try
            {
                var pos = Task.Run(() => GrpcPosFactory.CreatePosAsync(new GrpcClientOptions { Url = new Uri(url), RetryPolicyOptions = null })).Result;
                return Task.Run(() => pos.EchoAsync(new ifPOS.v1.EchoRequest { Message = message })).Result.Message;
            }
            catch (Exception ex)
            {
                Log.Error(AndroidLogger.TAG, ex.ToString());
                return string.Empty;
            }
        }

        [Export("SendSignTestBackdoor")]
        public string SendSignTestBackdoor(string url, string signRequest)
        {
            try
            {
                var pos = Task.Run(() => GrpcPosFactory.CreatePosAsync(new GrpcClientOptions { Url = new Uri(url), RetryPolicyOptions = null })).Result;
                var response = Task.Run(() => pos.SignAsync(JsonConvert.DeserializeObject<ReceiptRequest>(signRequest))).Result;

                return JsonConvert.SerializeObject(response);
            }
            catch (Exception ex)
            {
                Log.Error(AndroidLogger.TAG, ex.ToString());
                return string.Empty;
            }
        }
    }
}