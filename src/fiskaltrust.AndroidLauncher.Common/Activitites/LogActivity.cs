using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using fiskaltrust.AndroidLauncher.Common.Helpers.Logging;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Essentials;
using static System.Net.Mime.MediaTypeNames;

namespace fiskaltrust.AndroidLauncher.Common.Activitites
{
    [Activity(Label = "LogActivity", Theme = "@style/AppTheme")]
    public class LogActivity : Activity
    {
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.top_menus_logs, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Resource.Id.menuClearConsole)
            {
                TextView txt = FindViewById<TextView>(Resource.Id.txtLog);
                txt.Text = Resources.GetString(Resource.String.no_logs);
            }

            return base.OnOptionsItemSelected(item);
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_logs);

            TextView txt = FindViewById<TextView>(Resource.Id.txtLog);
            ScrollView scrollView = FindViewById<ScrollView>(Resource.Id.txtScrollView);
            if (txt == null) return;

            var lines = FileLoggerHelper.GetLastLinesOfCurrentLogFile(1024);
            txt.Text = string.IsNullOrEmpty(lines) ? Resources.GetString(Resource.String.no_logs) : lines;
            scrollView?.Post(() => scrollView.FullScroll(FocusSearchDirection.Down));
        }
    }
}