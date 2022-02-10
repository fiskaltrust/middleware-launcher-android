using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Java.Interop;

namespace fiskaltrust.AndroidLauncher.Common.Activitites
{
    [Activity(Label = "ManageSpaceActivity", Name = "eu.fiskaltrust.androidlauncher.common.ManageSpaceActivity")]
    public class ManageSpaceActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_manage_space);
        }

        [Export("buttonClearDataOnCLick")]
        public void ButtonClearDataOnCLick(View v)
        {
            ((ActivityManager)GetSystemService(ActivityService)).ClearApplicationUserData();
        }
    }
}