using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using fiskaltrust.AndroidLauncher.Common.Helpers.Logging;

namespace fiskaltrust.AndroidLauncher.Common.Activitites
{
    [Activity(Label = "LogActivity")]
    public class LogActivity : Activity
    {
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