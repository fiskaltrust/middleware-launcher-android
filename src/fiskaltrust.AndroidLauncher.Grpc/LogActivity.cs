using Android.App;
using Android.OS;
using Android.Widget;
using fiskaltrust.AndroidLauncher.Common.Helpers.Logging;
using Android.Views;

namespace fiskaltrust.AndroidLauncher.Grpc
{
    [Activity(Label = "LogActivity")]
    public class LogActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Common.Resource.Layout.activity_logs);

            TextView txt = FindViewById<TextView>(Common.Resource.Id.txtLog);
            ScrollView scrollView = FindViewById<ScrollView>(Common.Resource.Id.txtScrollView);
            if (txt == null) return;

            var lines = FileLoggerHelper.GetLastLinesOfCurrentLogFile(1024);
            txt.Text = string.IsNullOrEmpty(lines) ? "No log file found." : lines;
            scrollView?.FullScroll(FocusSearchDirection.Down);
        }
    }
}