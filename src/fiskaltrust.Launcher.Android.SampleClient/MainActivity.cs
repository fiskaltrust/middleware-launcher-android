using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Content.PM;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using Android.Widget;
using Newtonsoft.Json;
using Android.Media;

namespace fiskaltrust.Launcher.Android.SampleClient
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private AndroidLauncher _launcher;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            FindViewById<Button>(Resource.Id.btnInitLauncher).Click += ButtonInitOnClick;
            FindViewById<Button>(Resource.Id.btnSendEchoRequest).Click += ButtonEchoRequestOnClick;
            FindViewById<Button>(Resource.Id.btnSendSignRequest).Click += ButtonSignRequestOnClick;
            FindViewById<Button>(Resource.Id.btnSendStartReceipt).Click += ButtonStartReceiptOnClick;
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void ButtonInitOnClick(object sender, EventArgs eventArgs)
        {
            var x = System.Text.Encoding.Default;
            _launcher = new AndroidLauncher(Guid.Empty);
            Task.Run(() => _launcher.StartAsync()).Wait();

            Toast.MakeText(Application.Context, "fiskaltrust Android launcher started.", ToastLength.Long).Show();
        }

        private void ButtonEchoRequestOnClick(object sender, EventArgs eventArgs)
        {
            TextView txt = FindViewById<TextView>(Resource.Id.txtResult);

            if (_launcher == null)
            {
                txt.Text = "Please initialize launcher before sending requests.";
                return;
            }

            var pos = _launcher.GetPOS();
            var response = Task.Run(() => pos.EchoAsync(new EchoRequest { Message = $"Hello World, it's {DateTime.Now}!" })).Result;

            txt.Text = response.Message;
        }

        private void ButtonSignRequestOnClick(object sender, EventArgs eventArgs)
        {
            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = "82d3d0ed-ff0b-4aeb-9f1b-389f7d6b5b14",
                ftReceiptCase = 0x4445_0001_0000_0000,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbChargeItems = Array.Empty<ChargeItem>(),
                cbPayItems = Array.Empty<PayItem>()
            };
            TextView txt = FindViewById<TextView>(Resource.Id.txtSignResult);

            if (_launcher == null)
            {
                txt.Text = "Please initialize launcher before sending requests.";
                return;
            }

            var pos = _launcher.GetPOS();
            var response = Task.Run(() => pos.SignAsync(receiptRequest)).Result;

            txt.Text = JsonConvert.SerializeObject(response);
        }


        private void ButtonStartReceiptOnClick(object sender, EventArgs eventArgs)
        {
            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = "82d3d0ed-ff0b-4aeb-9f1b-389f7d6b5b14",
                ftQueueID = "b80af2a1-b7f8-4aa4-938f-9043b3a5ae40",
                ftPosSystemId = "d4a62055-ca6c-4372-ae4d-f835a88e4a5d",
                cbTerminalID = "T1",
                cbReceiptReference = "2020020120152812",
                cbReceiptMoment = DateTime.UtcNow,
                ftReceiptCaseData = "",
                cbUser = "Receptionist",
                cbArea = "System",
                cbSettlement = "",
                ftReceiptCase = 4919338172267102211,
                cbChargeItems = Array.Empty<ChargeItem>(),
                cbPayItems = Array.Empty<PayItem>()
            };
            TextView txt = FindViewById<TextView>(Resource.Id.txtStartReceiptResult);

            if (_launcher == null)
            {
                txt.Text = "Please initialize launcher before sending requests.";
                return;
            }

            var pos = _launcher.GetPOS();
            var response = Task.Run(() => pos.SignAsync(receiptRequest)).Result;

            txt.Text = JsonConvert.SerializeObject(response);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}
