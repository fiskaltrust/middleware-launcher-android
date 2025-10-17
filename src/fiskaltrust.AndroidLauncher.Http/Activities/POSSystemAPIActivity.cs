using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Widget;
using fiskaltrust.AndroidLauncher.Common;
using fiskaltrust.AndroidLauncher.Common.Constants;
using fiskaltrust.AndroidLauncher.Common.Helpers.Logging;
using fiskaltrust.AndroidLauncher.Common.Hosting;
using fiskaltrust.AndroidLauncher.Common.Services;
using fiskaltrust.AndroidLauncher.Http.Helpers;
using fiskaltrust.AndroidLauncher.Http.Hosting;
using fiskaltrust.ifPOS.v1;
using Java.Nio.Channels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Http.Broadcasting
{
    [Activity(Label = "POSSystemAPI", Enabled = true, Exported = true, Name = BroadcastConstants.POSSystemAPIApiBroadcastName)]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault })]
    public class POSSystemAPIActivity : Activity
    {
        private MiddlewareLauncher _launcher;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            try
            {
                var headersJson = Intent.GetStringExtra("headers");
                var headerPairs = JsonConvert.DeserializeObject<Dictionary<string, string>>(headersJson ?? "{}");
                var method = Intent.GetStringExtra("method");
                var body = Intent.GetStringExtra("body");
                var responseAction = Intent.GetStringExtra("responseAction");
                var requestId = Intent.GetStringExtra("requestId");
                var endpoint = Intent.GetStringExtra("endpoint");
                var pos = GetIPOS(headerPairs);
                if (endpoint.ToLower() == "/v2/sign")
                {
                    var test = Task.Run(async () => await pos.SignAsync(JsonConvert.DeserializeObject<ReceiptRequest>(body))).Result;
                    var responseIntent = new Intent(responseAction);
                    responseIntent.PutExtra("requestId", requestId);
                    responseIntent.PutExtra("statusCode", 200);
                    responseIntent.PutExtra("body", JsonConvert.SerializeObject(test));
                    SetResult(Result.Ok, responseIntent);
                    Finish();
                }
                else if (endpoint.ToLower() == "/v2/echo")
                {
                    var test = Task.Run(async () => await pos.EchoAsync(JsonConvert.DeserializeObject<EchoRequest>(body))).Result;
                    var responseIntent = new Intent(responseAction);
                    responseIntent.PutExtra("requestId", requestId);
                    responseIntent.PutExtra("statusCode", 200);
                    responseIntent.PutExtra("body", JsonConvert.SerializeObject(test));
                    SetResult(Result.Ok, responseIntent);
                    Finish();
                }
                else
                {
                    throw new Exception("Unsupported endpoint");
                }
            }
            catch (Exception ex)
            {
                Log.Error(AndroidLogger.TAG, $"Error processing Echo API intent: {ex}");

                // Send error response if possible
                var responseAction = Intent.GetStringExtra("responseAction");
                var requestId = Intent.GetStringExtra("requestId");

                SetResult(Result.Canceled);
                Finish();
            }
        }

        private IPOS GetIPOS(Dictionary<string, string> headerPairs)
        {
            if (_launcher == null)
            {
                _launcher = new MiddlewareLauncher(new DummyHostFactory(), new RestUrlResolver(), Guid.Parse(headerPairs["x-cashbox-id"]), headerPairs["x-cashbox-accesstoken"], true, Microsoft.Extensions.Logging.LogLevel.Debug, new Dictionary<string, object>());
            }
            if (!_launcher.IsRunning)
            {
                Task.Run(async () => await _launcher.StartAsync()).Wait();
            }
            var pos = _launcher._poss.First();
            return pos;
        }
    }

    public class DummyHost : IHost<IPOS>
    {
        public Task StartAsync(string url, IPOS instance, Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            return Task.CompletedTask;
        }
        public Task StopAsync()
        {
            return Task.CompletedTask;
        }
    }

    public class DummyHostFactory : IHostFactory
    {
        public IHost<IPOS> CreatePosHost()
        {
            return new DummyHost();
        }
    }
}