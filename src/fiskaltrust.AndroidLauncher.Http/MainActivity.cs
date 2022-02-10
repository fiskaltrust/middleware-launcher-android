using Android.App;
using Java.Interop;
using Android.Content;
using fiskaltrust.Middleware.Interface.Client.Http;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.AndroidLauncher.Common.Helpers.Logging;
using fiskaltrust.AndroidLauncher.Common.Activitites;
using Log = Android.Util.Log;

namespace fiskaltrust.AndroidLauncher.Http
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : BaseMainActivity
    {
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