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

            Button btnInit = FindViewById<Button>(Resource.Id.btnInitLauncher);
            btnInit.Click += ButtonInitOnClick;
            Button btnEchoReq = FindViewById<Button>(Resource.Id.btnSendEchoRequest);
            btnEchoReq.Click += ButtonEchoRequestOnClick;
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
            _launcher = new AndroidLauncher(Guid.Empty);
            _launcher.StartAsync().Wait();

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

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
	}
}
