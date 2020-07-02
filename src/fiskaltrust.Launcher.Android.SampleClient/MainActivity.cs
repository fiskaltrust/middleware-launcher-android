using System;
using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using Android.Widget;
using Newtonsoft.Json;
using Java.Lang;
using fiskaltrust.AndroidLauncher.Exceptions;
using Android.Content;

namespace fiskaltrust.AndroidLauncher.SampleClient
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private MiddlewareServiceConnection _serviceConnection;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            FindViewById<Button>(Resource.Id.btnSendEchoRequest).Click += new EventHandler(async (s, e) => await ButtonEchoRequestOnClickAsync());
            FindViewById<Button>(Resource.Id.btnSendSignRequest).Click += new EventHandler(async (s, e) => await ButtonSignRequestOnClickAsync());
            FindViewById<Button>(Resource.Id.btnSendStartReceipt).Click += new EventHandler(async (s, e) => await ButtonStartReceiptOnClickAsync());
            FindViewById<Button>(Resource.Id.btnSendZeroReceipt).Click += new EventHandler(async (s, e) => await ButtonZeroReceiptOnClickAsync());


            if (_serviceConnection == null)
            {
                _serviceConnection = new MiddlewareServiceConnection(this);
            }

            Intent serviceToStart = new Intent(this, typeof(MiddlewareLauncherService));
            BindService(serviceToStart, _serviceConnection, Bind.AutoCreate);
            this.StartForegroundServiceCompat<MiddlewareLauncherService>();
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

        public void UpdateUiForUnboundService()
        {
            var label = FindViewById<TextView>(Resource.Id.txtServiceStatus);
            label.SetTextColor(Android.Graphics.Color.Red);
            label.Text = "Not connected to Middleware service.";
            SetButtonsEnabled(false);
        }

        public void UpdateUiForBoundService()
        {
            var label = FindViewById<TextView>(Resource.Id.txtServiceStatus);
            label.SetTextColor(Android.Graphics.Color.DarkGreen);
            label.Text = "Connected to Middleware service.";
            SetButtonsEnabled(true);
        }

        private async Task ButtonEchoRequestOnClickAsync()
        {
            SetButtonsEnabled(false);
            TextView txt = FindViewById<TextView>(Resource.Id.txtResult);
             

            var pos = await _serviceConnection.GetPOSAsync();
            var response = await pos.EchoAsync(new EchoRequest { Message = $"Hello World, it's {DateTime.Now}!" });

            txt.Text = response.Message;
            SetButtonsEnabled(true);
        }

        private async Task ButtonSignRequestOnClickAsync()
        {
            SetButtonsEnabled(false);
            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = "82d3d0ed-ff0b-4aeb-9f1b-389f7d6b5b14",
                ftReceiptCase = 0x4445_0001_0000_0000,
                cbReceiptReference = Guid.NewGuid().ToString(),
                cbChargeItems = Array.Empty<ChargeItem>(),
                cbPayItems = Array.Empty<PayItem>()
            };
            TextView txt = FindViewById<TextView>(Resource.Id.txtSignResult);

            var pos = await _serviceConnection.GetPOSAsync();
            var response = await pos.SignAsync(receiptRequest);

            txt.Text = JsonConvert.SerializeObject(response, Formatting.Indented);
            SetButtonsEnabled(true);
        }

        private async Task ButtonStartReceiptOnClickAsync()
        {
            SetButtonsEnabled(false);
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
                ftReceiptCase = 0x4445_0001_0000_0003,
                cbChargeItems = Array.Empty<ChargeItem>(),
                cbPayItems = Array.Empty<PayItem>()
            };
            TextView txt = FindViewById<TextView>(Resource.Id.txtSpecialReceiptResult);

            var pos = await _serviceConnection.GetPOSAsync();
            var response = await pos.SignAsync(receiptRequest);

            txt.Text = JsonConvert.SerializeObject(response, Formatting.Indented);
            SetButtonsEnabled(true);
        }

        private async Task ButtonZeroReceiptOnClickAsync()
        {
            SetButtonsEnabled(false);
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
                ftReceiptCase = 0x4445_0001_0000_0002,
                cbChargeItems = Array.Empty<ChargeItem>(),
                cbPayItems = Array.Empty<PayItem>()
            };
            TextView txt = FindViewById<TextView>(Resource.Id.txtSpecialReceiptResult);

            var pos = await _serviceConnection.GetPOSAsync();
            var response = await pos.SignAsync(receiptRequest);

            txt.Text = JsonConvert.SerializeObject(response, Formatting.Indented);
            SetButtonsEnabled(true);
        }

        private void SetButtonsEnabled(bool state)
        {
            FindViewById<Button>(Resource.Id.btnSendEchoRequest).Enabled = state;
            FindViewById<Button>(Resource.Id.btnSendSignRequest).Enabled = state;
            FindViewById<Button>(Resource.Id.btnSendStartReceipt).Enabled = state;
            FindViewById<Button>(Resource.Id.btnSendZeroReceipt).Enabled = state;
        }
    }
}
