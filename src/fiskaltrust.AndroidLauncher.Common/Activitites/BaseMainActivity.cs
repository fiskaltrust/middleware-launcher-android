using System.IO;
using System.Threading.Tasks;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using fiskaltrust.AndroidLauncher.Common.Helpers.Logging;
using Xamarin.Essentials;

namespace fiskaltrust.AndroidLauncher.Common.Activitites
{
    public abstract class BaseMainActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            VersionTracking.Track();
            if (FileLoggerHelper.LogDirectory.Exists == false) FileLoggerHelper.LogDirectory.Create();

            SetContentView(Resource.Layout.activity_main);

            Init();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.top_menus, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Resource.Id.menuLogs)
            {
                StartActivity(typeof(LogActivity));
            }
            else if (item.ItemId == Resource.Id.menuCopyLogs)
            {
                Task.Run(async () => await CopyLogs());
            }

            return base.OnOptionsItemSelected(item);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void Init()
        {
            FindViewById<TextView>(Resource.Id.textViewVersion).Text = $"Version {VersionTracking.CurrentVersion}";
        }

        private async Task CopyLogs()
        {
            var hasPermission = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
            if (hasPermission != PermissionStatus.Granted)
            {
                hasPermission = await MainThread.InvokeOnMainThreadAsync(async () => await Permissions.RequestAsync<Permissions.StorageWrite>());
            }

            if (hasPermission != PermissionStatus.Granted) return;

            var targetPath = ApplicationContext?.GetExternalFilesDir(null)?.AbsolutePath;
            if (targetPath != null)
            {
                var targetDir = new DirectoryInfo(Path.Combine(targetPath, "logs"));
                if (!targetDir.Exists) targetDir.Create();

                foreach (var file in FileLoggerHelper.LogDirectory.GetFiles("*.log"))
                {
                    var destFile = new FileInfo(Path.Combine(targetDir.FullName, file.Name));
                    if (destFile.Exists) destFile.Delete();
                    await CopyFileAsync(file.FullName, destFile.FullName);
                }

                RunOnUiThread(() => new AlertDialog.Builder(this)
                    .SetTitle("Success")
                    .SetMessage($"The log files were copied to '{targetDir}'.")
                    .SetNeutralButton("OK", (sender, args) => { })
                    .Show());
            }
        }

        private async Task CopyFileAsync(string sourcePath, string destinationPath)
        {
            await using Stream source = File.Open(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            await using Stream destination = File.Create(destinationPath);
            await source.CopyToAsync(destination);
        }
    }
}