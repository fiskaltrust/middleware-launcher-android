using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.SmokeTests
{
    /// <summary>
    /// Integration test helper for testing PosSystemAPI Intent calls.
    /// This can be used in instrumented tests to verify the Intent-based API.
    /// </summary>
    public class PosSystemAPIIntentTestHelper
    {
        private const string TAG = "PosSystemAPITest";
        private const int REQUEST_CODE = 9999;
        private Activity _activity;
        private TaskCompletionSource<IntentResult> _resultTcs;

        public PosSystemAPIIntentTestHelper(Activity activity)
        {
            _activity = activity;
        }

        /// <summary>
        /// Sends an Intent to the PosSystemAPI and waits for the result
        /// </summary>
        public async Task<IntentResult> SendIntentAsync(string method, string path, Dictionary<string, string> headers, string body = null)
        {
            _resultTcs = new TaskCompletionSource<IntentResult>();

            try
            {
                // Encode headers
                var headersJson = JsonConvert.SerializeObject(headers);
                var headersBase64Url = ToBase64Url(headersJson);

                // Encode body if present
                string bodyBase64Url = null;
                if (!string.IsNullOrEmpty(body))
                {
                    bodyBase64Url = ToBase64Url(body);
                }

                // Create intent
                var intent = new Intent();
                intent.SetClassName("eu.fiskaltrust.androidlauncher", "eu.fiskaltrust.androidlauncher.PosSystemAPI");
                intent.PutExtra("Method", method);
                intent.PutExtra("Path", path);
                intent.PutExtra("HeaderJsonObjectBase64Url", headersBase64Url);

                if (!string.IsNullOrEmpty(bodyBase64Url))
                {
                    intent.PutExtra("BodyBase64Url", bodyBase64Url);
                }

                Log.Info(TAG, $"Sending Intent: {method} {path}");
                _activity.StartActivityForResult(intent, REQUEST_CODE);

                // Wait for result (with timeout)
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
                var resultTask = _resultTcs.Task;

                var completedTask = await Task.WhenAny(resultTask, timeoutTask);
                if (completedTask == timeoutTask)
                {
                    throw new TimeoutException("Intent call timed out after 30 seconds");
                }

                return await resultTask;
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"Error sending Intent: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Call this from the Activity's OnActivityResult method
        /// </summary>
        public void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (requestCode != REQUEST_CODE)
            {
                return;
            }

            try
            {
                if (data == null)
                {
                    _resultTcs?.TrySetException(new Exception("No result data received"));
                    return;
                }

                var statusCode = data.GetStringExtra("StatusCode") ?? "500";
                var contentB64 = data.GetStringExtra("ContentBase64Url") ?? "";
                var contentTypeB64 = data.GetStringExtra("ContentTypeBase64Url") ?? "";
                var headerB64 = data.GetStringExtra("HeaderJsonObjectBase64Url") ?? "";

                var result = new IntentResult
                {
                    StatusCode = statusCode,
                    Content = FromBase64Url(contentB64),
                    ContentType = FromBase64Url(contentTypeB64),
                    Headers = string.IsNullOrEmpty(headerB64) ? null : JsonConvert.DeserializeObject<Dictionary<string, string>>(FromBase64Url(headerB64))
                };

                Log.Info(TAG, $"Received result: Status={result.StatusCode}, ContentType={result.ContentType}");
                Log.Debug(TAG, $"Content: {result.Content}");

                _resultTcs?.TrySetResult(result);
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"Error processing result: {ex}");
                _resultTcs?.TrySetException(ex);
            }
        }

        private string ToBase64Url(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var bytes = Encoding.UTF8.GetBytes(text);
            var base64 = Convert.ToBase64String(bytes);
            return base64
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private string FromBase64Url(string base64Url)
        {
            if (string.IsNullOrEmpty(base64Url))
                return string.Empty;

            var base64 = base64Url
                .Replace('-', '+')
                .Replace('_', '/');

            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }

            var bytes = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(bytes);
        }

        public class IntentResult
        {
            public string StatusCode { get; set; }
            public string Content { get; set; }
            public string ContentType { get; set; }
            public Dictionary<string, string> Headers { get; set; }
        }
    }

#if DEBUG
    /// <summary>
    /// Example test activity that demonstrates how to test the PosSystemAPI Intent interface.
    /// This is for development/debugging purposes only.
    /// </summary>
    [Activity(Label = "PosSystemAPI Intent Tests", Exported = false)]
    public class PosSystemAPIIntentTestActivity : Activity
    {
        private const string TAG = "PosSystemAPIIntentTest";
        private PosSystemAPIIntentTestHelper _testHelper;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _testHelper = new PosSystemAPIIntentTestHelper(this);

            // Run tests
            Task.Run(async () => await RunTestsAsync());
        }

        private async Task RunTestsAsync()
        {
            try
            {
                Log.Info(TAG, "Starting Intent API tests...");

                // Test 1: Echo endpoint
                await TestEchoEndpoint();

                // Test 2: Invalid method
                await TestInvalidMethod();

                // Test 3: Missing required parameters
                await TestMissingParameters();

                Log.Info(TAG, "All tests completed!");
            }
            catch (Exception ex)
            {
                Log.Error(TAG, $"Tests failed: {ex}");
            }
        }

        private async Task TestEchoEndpoint()
        {
            Log.Info(TAG, "Test: Echo endpoint");

            var headers = new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Content-Type", "application/json" },
                { "x-cashbox-id", "test-cashbox-id" },
                { "x-cashbox-accesstoken", "test-token" },
                { "x-operation-id", Guid.NewGuid().ToString() }
            };

            var body = JsonConvert.SerializeObject(new { Message = "Test Echo" });

            var result = await _testHelper.SendIntentAsync("POST", "/echo", headers, body);

            if (result.StatusCode == "200" || result.StatusCode == "201")
            {
                Log.Info(TAG, "✓ Echo test passed");
            }
            else
            {
                Log.Error(TAG, $"✗ Echo test failed: Status={result.StatusCode}, Content={result.Content}");
            }
        }

        private async Task TestInvalidMethod()
        {
            Log.Info(TAG, "Test: Invalid HTTP method");

            var headers = new Dictionary<string, string>
            {
                { "Accept", "application/json" }
            };

            var result = await _testHelper.SendIntentAsync("INVALID_METHOD", "/echo", headers);

            if (result.StatusCode != "200" && result.StatusCode != "201")
            {
                Log.Info(TAG, "✓ Invalid method test passed");
            }
            else
            {
                Log.Error(TAG, "✗ Invalid method test failed: Should have returned error");
            }
        }

        private async Task TestMissingParameters()
        {
            Log.Info(TAG, "Test: Missing required parameters");

            var headers = new Dictionary<string, string>
            {
                { "Accept", "application/json" }
            };

            try
            {
                var result = await _testHelper.SendIntentAsync("POST", null, headers);

                if (result.StatusCode == "400")
                {
                    Log.Info(TAG, "✓ Missing parameters test passed");
                }
                else
                {
                    Log.Error(TAG, $"✗ Missing parameters test failed: Expected 400, got {result.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Log.Info(TAG, $"✓ Missing parameters test passed (caught exception: {ex.Message})");
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            _testHelper.OnActivityResult(requestCode, resultCode, data);
        }
    }
#endif
}
