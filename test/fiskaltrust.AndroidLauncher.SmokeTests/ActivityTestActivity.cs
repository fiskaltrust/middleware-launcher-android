using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;

namespace fiskaltrust.AndroidLauncher.SmokeTests
{
    /// <summary>
    /// Calls the launcher's intent-based PosSystemAPI activity via StartActivityForResult.
    /// The result is reported to the pending <see cref="SendRequest"/> call (when launched
    /// through it, same process) and logged to logcat (for manual runs).
    /// </summary>
    [Activity(
        Name = "eu.fiskaltrust.androidlauncher.smoketests.ActivityTestActivity",
        Exported = true)]
    public class ActivityTestActivity : Activity
    {
        private const string TAG = "ActivityTest";
        private const int RequestCode = 1;

        private const string LauncherPackage = "eu.fiskaltrust.androidlauncher";
        private const string LauncherActivityClass = "eu.fiskaltrust.androidlauncher.PosSystemAPI";

        private static TaskCompletionSource<string>? _pendingResult;

        /// <summary>
        /// Launches this activity to send a single request through the intent-based
        /// PosSystemAPI surface and waits synchronously for the result. Returns the
        /// StatusCode; throws on launch failures, timeouts, or a result without one.
        /// </summary>
        internal static string SendRequest(Context context, PosSystemApiRequest request)
        {
            var pendingResult = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingResult = pendingResult;

            try
            {
                var intent = new Intent(context, typeof(ActivityTestActivity));
                // ClearTask forces a fresh instance (and OnCreate) even when the previous
                // test's instance is still finishing. Without it, a back-to-back launch can
                // resolve as START_DELIVERED_TO_TOP into the dying instance, silently
                // dropping the intent (observed on slow CI emulators).
                intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
                request.WriteTo(intent);
                context.StartActivity(intent);

                if (!pendingResult.Task.Wait(TimeSpan.FromMinutes(5)))
                {
                    throw new TimeoutException($"Timed out waiting for result from PosSystemAPI activity after 5min");
                }

                return pendingResult.Task.GetAwaiter().GetResult();
            }
            finally
            {
                _pendingResult = null;
            }
        }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var request = PosSystemApiRequest.FromExtras(name => Intent?.GetStringExtra(name));

            var intent = new Intent();
            intent.SetClassName(LauncherPackage, LauncherActivityClass);
            request.WriteTo(intent);

            try
            {
                StartActivityForResult(intent, RequestCode);
            }
            catch (Exception ex)
            {
                ReportError(new InvalidOperationException($"Failed to start {LauncherActivityClass}: {ex.Message}", ex));
                Finish();
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode != RequestCode)
            {
                return;
            }

            var statusCode = data?.GetStringExtra(PosSystemApiRequest.KeyStatusCode);
            if (statusCode != null)
            {
                Log.Info(TAG, $"RESULT={statusCode}");
                _pendingResult?.TrySetResult(statusCode);
            }
            else
            {
                ReportError(new InvalidOperationException($"No StatusCode in result (resultCode={resultCode})"));
            }

            Finish();
        }

        private static void ReportError(Exception ex)
        {
            Log.Info(TAG, $"RESULT=ERROR:{ex.Message}");
            _pendingResult?.TrySetException(ex);
        }
    }
}
