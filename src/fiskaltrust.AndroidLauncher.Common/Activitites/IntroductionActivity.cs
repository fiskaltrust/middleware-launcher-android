using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using fiskaltrust.AndroidLauncher.Common.Helpers;
using Java.Interop;
using Microsoft.Extensions.Primitives;
using System.Linq;
using System.Threading;

namespace fiskaltrust.AndroidLauncher.Common.Activitites
{
    [Activity(Label = "IntroductionActivity", Name = "eu.fiskaltrust.androidlauncher.common.IntroductionActivity", Exported = true)]
    public class IntroductionActivity : Activity
    {

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_introduction);

            FindViewById<Button>(Resource.Id.buttonRequestNotification).Enabled = !NotificationPermissionHelper.IsAllowingNotifications(this);

            FindViewById<Button>(Resource.Id.buttonRequestBatteryOptimization).Enabled = !PowerManagerHelper.IsIgnoringBatteryOptimizations(this);
        }

        [Export("buttonRequestNotificationOnCLick")]
        public void ButtonRequestNotificationOnCLick(View v)
        {
            NotificationPermissionHelper.AskUserToAllowNotifications(this, 2);

        }

        [Export("buttonRequestBatteryOptimizationOnCLick")]
        public void ButtonRequestBatteryOptimizationOnCLick(View v)
        {
            PowerManagerHelper.AskUserToDisableBatteryOptimization(this, 1);
            FindViewById<Button>(Resource.Id.buttonRequestBatteryOptimization).Enabled = !PowerManagerHelper.IsIgnoringBatteryOptimizations(this);
        }

        protected override void OnResume()
        {
            base.OnResume();
            FindViewById<Button>(Resource.Id.buttonRequestBatteryOptimization).Enabled = !PowerManagerHelper.IsIgnoringBatteryOptimizations(this);
            FindViewById<Button>(Resource.Id.buttonRequestNotification).Enabled = !NotificationPermissionHelper.IsAllowingNotifications(this);

            TryContinue();
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (resultCode == Result.Ok)
            {
                // this activity normally does not return a result so we'll probably never run into the 1 branch but let's keep if for good measure
                if (requestCode == 1)
                {
                    FindViewById<Button>(Resource.Id.buttonRequestBatteryOptimization).Enabled = !PowerManagerHelper.IsIgnoringBatteryOptimizations(this);
                }
                if (requestCode == 2)
                {
                    FindViewById<Button>(Resource.Id.buttonRequestNotification).Enabled = !NotificationPermissionHelper.IsAllowingNotifications(this);
                }
                TryContinue();
            }
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            if (grantResults[0] == Android.Content.PM.Permission.Denied)
            {
                FindViewById<Button>(Resource.Id.buttonRequestNotification).Enabled = !NotificationPermissionHelper.IsAllowingNotifications(this);
                TryContinue();
            }
        }


        private void TryContinue()
        {
            if (
                PowerManagerHelper.IsIgnoringBatteryOptimizations(this)
                &&
                NotificationPermissionHelper.IsAllowingNotifications(this)
            )
            {
                SetResult(0);
                Finish();
            }
        }
    }
}