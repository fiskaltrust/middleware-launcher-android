using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using System;

namespace fiskaltrust.AndroidLauncher.Common.Helpers
{
    public static class NotificationPermissionHelper
    {
        public static void AskUserToAllowNotifications(Activity activity, int requestCode)
        {
            if (!IsAllowingNotifications(activity.ApplicationContext))
            {
                activity.RequestPermissions(new[]{Android.Manifest.Permission.PostNotifications}, requestCode);
            }
        }

        public static bool IsAllowingNotifications(Context context)
        {
            return context.CheckSelfPermission(Android.Manifest.Permission.PostNotifications) == Permission.Granted;
        }
    }
}