using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using fiskaltrust.AndroidLauncher.Common;
using System;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using Android.Content;

namespace fiskaltrust.AndroidLauncher
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private const string CASHBOX_ID = "4481f82c-a167-4578-832f-a0948c22c3c4";
        private const string ACCESS_TOKEN = "BDnVd83nE4yHdla1e92ecyGuFyMeyAVLt78ttMLPjsvPgUTzyjUlzX6LIP1wc14Bbsj2LVH3Dzqwucc763lGVDE=";
        private MiddlewareServiceConnection _serviceConnection;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            FindViewById<Button>(Resource.Id.btnClick).Click += new EventHandler(async (s, e) => await OnClick(s, e));

            //_serviceConnection = new MiddlewareServiceConnection();
            //MiddlewareLauncherService.Start(_serviceConnection, CASHBOX_ID, ACCESS_TOKEN);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private async Task OnClick(object sender, System.EventArgs e)
        {

            //var message = new Intent("eu.fiskaltrust.AndroidLauncher.Start");
            //message.PutExtra("cashboxid", CASHBOX_ID);
            //message.PutExtra("accesstoken", ACCESS_TOKEN);
            //SendBroadcast(message);


            var intent = new Intent();
            intent.PutExtra("cashboxid", CASHBOX_ID);
            intent.PutExtra("accesstoken", ACCESS_TOKEN);
            var c = Java.Lang.Class.FromType(typeof(MiddlewareLauncherService));

            intent.SetComponent(new ComponentName(c.Package.Name, c.Name));

            StartForegroundService(intent);

            //var pos = await _serviceConnection.GetPOSAsync();
            //var echoResponse = await pos.EchoAsync(new EchoRequest { Message = $"Hello World, it's {DateTime.Now}!" });

            //var receiptRequest = new ReceiptRequest
            //{
            //    ftCashBoxID = CASHBOX_ID,
            //    ftPosSystemId = "d4a62055-ca6c-4372-ae4d-f835a88e4a5d",
            //    cbTerminalID = "T1",
            //    cbReceiptReference = "2020020120152812",
            //    cbReceiptMoment = DateTime.UtcNow,
            //    ftReceiptCaseData = "",
            //    cbUser = "Receptionist",
            //    cbArea = "System",
            //    cbSettlement = "",
            //    ftReceiptCase = 0x4445_0001_0000_0003,
            //    cbChargeItems = Array.Empty<ChargeItem>(),
            //    cbPayItems = Array.Empty<PayItem>()
            //};
            //var response = await pos.SignAsync(receiptRequest);

        }
    }
}