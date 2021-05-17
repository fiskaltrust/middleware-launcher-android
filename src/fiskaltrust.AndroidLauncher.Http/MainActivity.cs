using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Java.Interop;
using Android.Content;
using Android.Widget;
using fiskaltrust.Middleware.Interface.Client.Http;
using System;
using fiskaltrust.Middleware.Interface.Client;
using System.Threading.Tasks;
using Newtonsoft.Json;
using fiskaltrust.ifPOS.v1;
using Xamarin.Essentials;
using Android.Util;
using fiskaltrust.AndroidLauncher.Common.Helpers.Logging;
using fiskaltrust.AndroidLauncher.Http.Broadcasting;

namespace fiskaltrust.AndroidLauncher.Http
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private StopLauncherBroadcastReceiver _stopLauncherBroadcastReceiver;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            VersionTracking.Track();

            SetContentView(Common.Resource.Layout.activity_main);

            _stopLauncherBroadcastReceiver = new StopLauncherBroadcastReceiver();
            _stopLauncherBroadcastReceiver.StopLauncherReceived += delegate {
                Android.Widget.Toast.MakeText(Application.Context, "Stopped fiskaltrust.Launcher", Android.Widget.ToastLength.Long).Show();

                Finish();
                FinishAffinity();

                Java.Lang.JavaSystem.Exit(0);
            };

            RegisterReceiver(_stopLauncherBroadcastReceiver, new IntentFilter(Common.Constants.BroadcastConstants.HttpStopBroadcastName));

            Init();

            SendStartIntentTestBackdoor("c7bdfec2-1c99-48d6-9ce0-5caaf613cd0b", "BBzMuxESso0z6h7Od/imq8wLCYYO2jkDfGVpd2q+9BbC/GttKt1Iqj0u7uE8LOpd74EnKYSZMg6Dim0ZeK2Yi+4=");
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
            var componentName = new ComponentName("eu.fiskaltrust.androidlauncher.http", "eu.fiskaltrust.androidlauncher.http.Start");

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
                var pos = Task.Run(() => HttpPosFactory.CreatePosAsync(new HttpPosClientOptions { Url = new Uri(url), RetryPolicyOptions = null })).Result;
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
                var pos = Task.Run(() => HttpPosFactory.CreatePosAsync(new HttpPosClientOptions { Url = new Uri(url), RetryPolicyOptions = null })).Result;
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