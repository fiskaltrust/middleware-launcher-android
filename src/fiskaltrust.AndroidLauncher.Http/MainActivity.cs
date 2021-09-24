using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Java.Interop;
using Android.Content;
using Android.Widget;
using fiskaltrust.Middleware.Interface.Client.Http;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using fiskaltrust.ifPOS.v1;
using Xamarin.Essentials;
using fiskaltrust.AndroidLauncher.Common.Helpers.Logging;
using Android.Views;
using Log = Android.Util.Log;

namespace fiskaltrust.AndroidLauncher.Http
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            VersionTracking.Track();
            if (FileLoggerHelper.LogDirectory.Exists == false) FileLoggerHelper.LogDirectory.Create();

            SetContentView(Common.Resource.Layout.activity_main);
            
            Init();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Common.Resource.Menu.top_menus, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Common.Resource.Id.menuLogs)
            {
                StartActivity(typeof(LogActivity));
            }
            return base.OnOptionsItemSelected(item);
        }
        
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void Init()
        {
            FindViewById<TextView>(Common.Resource.Id.textViewVersion).Text = $"Version {VersionTracking.CurrentVersion}";
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
            intent.PutExtra("enableCloseButton", true);

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