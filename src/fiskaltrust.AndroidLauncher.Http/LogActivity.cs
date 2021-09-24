using Android.App;
using Android.OS;
using Android.Widget;
using fiskaltrust.AndroidLauncher.Common.Helpers.Logging;
using System.IO;
using System.Linq;

namespace fiskaltrust.AndroidLauncher.Http
{
    [Activity(Label = "LogActivity")]
    public class LogActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Common.Resource.Layout.activity_logs);

            var files = new DirectoryInfo(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), FileLogger.LogDirectory))
                .GetFiles("*.log").OrderBy(f=>f.LastWriteTime);
            
            var txt = FindViewById<TextView>(Common.Resource.Id.txtLog);
            if (txt == null) return;
            txt.Text = files.Any() ? File.ReadAllText(files.First().FullName) : "No log file found.";
        }
    }
}