using Android.App;
using Android.OS;
using Android.Widget;
using fiskaltrust.AndroidLauncher.Common.Helpers.Logging;
using System.IO;

namespace fiskaltrust.AndroidLauncher.Http
{
    [Activity(Label = "LogActivity")]
    public class LogActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Common.Resource.Layout.activity_logs);

            var logFile = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), FileLogger.LogDirectory, FileLogger.LogFilename);

            var txt = FindViewById<TextView>(Common.Resource.Id.txtLog);
            if (File.Exists(logFile))
            {
                txt.Text = File.ReadAllText(logFile);
            }
            else
            {
                txt.Text = "No log file found.";
            }
        }
    }
}