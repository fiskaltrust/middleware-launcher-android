using System.Text;
using System.Text.Json;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;

namespace fiskaltrust.AndroidLauncher.PosSystemApiTestClient
{
    [Activity(
        Name = "eu.fiskaltrust.androidlauncher.testclient.ActivityTestActivity",
        Exported = true)]
    public class ActivityTestActivity : Activity
    {
        private const string TAG = "ActivityTest";
        private const int RequestCode = 1;

        private const string LauncherPackage = "eu.fiskaltrust.androidlauncher";
        private const string LauncherActivityClass = "eu.fiskaltrust.androidlauncher.PosSystemAPI";

        private const string ExtraMethod = "Method";
        private const string ExtraPath = "Path";
        private const string ExtraHeaderJsonBase64Url = "HeaderJsonObjectBase64Url";
        private const string ExtraBodyBase64Url = "BodyBase64Url";
        private const string ExtraStatusCode = "StatusCode";

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var method = Intent?.GetStringExtra(ExtraMethod) ?? "POST";
            var path = Intent?.GetStringExtra(ExtraPath) ?? "/v2/echo";
            var headerJsonBase64Url = Intent?.GetStringExtra(ExtraHeaderJsonBase64Url) ?? DefaultHeadersBase64Url();
            var bodyBase64Url = Intent?.GetStringExtra(ExtraBodyBase64Url) ?? ToBase64Url(JsonSerializer.Serialize(new { Message = "Ping" }));

            var intent = new Intent();
            intent.SetClassName(LauncherPackage, LauncherActivityClass);
            intent.PutExtra(ExtraMethod, method);
            intent.PutExtra(ExtraPath, path);
            intent.PutExtra(ExtraHeaderJsonBase64Url, headerJsonBase64Url);
            intent.PutExtra(ExtraBodyBase64Url, bodyBase64Url);

            StartActivityForResult(intent, RequestCode);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode != RequestCode)
            {
                return;
            }

            var statusCode = data?.GetStringExtra(ExtraStatusCode);
            Log.Info(TAG, $"RESULT={statusCode ?? "ERROR:no StatusCode in result"}");

            Finish();
        }

        private static string DefaultHeadersBase64Url()
        {
            var headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "x-cashbox-id", TestConstants.Http.CashboxId },
                { "x-cashbox-accesstoken", TestConstants.Http.AccessToken },
                { "x-operation-id", Guid.NewGuid().ToString() }
            };
            return ToBase64Url(JsonSerializer.Serialize(headers));
        }

        private static string ToBase64Url(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
