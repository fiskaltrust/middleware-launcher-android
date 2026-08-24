using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;

namespace fiskaltrust.AndroidLauncher.SmokeTests
{
    /// <summary>
    /// Runs the PosSystemAPI smoke test suite (see <see cref="SmokeTestSuite"/>) against
    /// the installed launcher and reports per-test results using the standard
    /// instrumentation status protocol, so results can be read from the output of:
    ///
    ///   adb shell am instrument -w -r \
    ///     [-e CashboxId ...] [-e AccessToken ...] [-e Test name-substring] \
    ///     eu.fiskaltrust.androidlauncher.smoketests/eu.fiskaltrust.androidlauncher.smoketests.TestInstrumentation
    ///
    /// The final instrumentation code is -1 (Activity.RESULT_OK) if all tests passed,
    /// 0 otherwise.
    /// </summary>
    [Instrumentation(
        Name = "eu.fiskaltrust.androidlauncher.smoketests.TestInstrumentation",
        TargetPackage = "eu.fiskaltrust.androidlauncher.smoketests")]
    public class TestInstrumentation : Instrumentation
    {
        private const string TAG = "TestInstrumentation";

        private const string ArgCashboxId = "CashboxId";
        private const string ArgAccessToken = "AccessToken";
        private const string ArgTestFilter = "Test";

        // Standard instrumentation status codes understood by `am instrument` and CI tooling.
        private const int StatusStart = 1;
        private const int StatusOk = 0;
        private const int StatusFailure = -2;

        private Bundle? _arguments;

        public TestInstrumentation()
        {
        }

        // Required so .NET for Android can activate the instance created by the framework.
        public TestInstrumentation(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
        }

        public override void OnCreate(Bundle? arguments)
        {
            base.OnCreate(arguments);
            _arguments = arguments;
            Start();
        }

        public override void OnStart()
        {
            base.OnStart();

            var cashboxId = _arguments?.GetString(ArgCashboxId) ?? TestConstants.DefaultCashboxId;
            var accessToken = _arguments?.GetString(ArgAccessToken) ?? TestConstants.DefaultAccessToken;
            var testFilter = _arguments?.GetString(ArgTestFilter);

            SmokeTestSuite.TargetContext = TargetContext ?? throw new InvalidOperationException("TargetContext is null");
            SmokeTestSuite.CashboxId = cashboxId;
            SmokeTestSuite.AccessToken = accessToken;

            var tests = SmokeTestSuite.Tests
                .Where(t => testFilter == null || t.Name.Contains(testFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var failures = 0;
            for (var i = 0; i < tests.Count; i++)
            {
                var test = tests[i];
                var current = i + 1;
                ReportStatus(StatusStart, test.Name, tests.Count, current, stream: $"{test.Name}: ");

                try
                {
                    Log.Info(TAG, $"Running {test.Name}");
                    test.Run();
                    Log.Info(TAG, $"{test.Name} passed");
                    ReportStatus(StatusOk, test.Name, tests.Count, current, stream: "OK\n");
                }
                catch (Exception ex)
                {
                    failures++;
                    Log.Warn(TAG, $"{test.Name} failed: {ex}");
                    ReportStatus(StatusFailure, test.Name, tests.Count, current, stream: $"FAILED\n{ex.Message}\n", stack: ex.ToString());
                }
            }

            var summary = failures > 0
                ? $"\nFAILURES: {failures} of {tests.Count} tests failed\n"
                : $"\nOK: {tests.Count} tests passed\n";
            var results = new Bundle();
            results.PutString("stream", summary);
            Finish(failures == 0 && tests.Count > 0 ? Result.Ok : Result.Canceled, results);
        }

        private void ReportStatus(int statusCode, string testName, int numTests, int current, string stream, string? stack = null)
        {
            var status = new Bundle();
            status.PutString("id", TAG);
            status.PutString("class", GetType().FullName);
            status.PutString("test", testName);
            status.PutInt("numtests", numTests);
            status.PutInt("current", current);
            status.PutString("stream", stream);
            if (stack != null)
            {
                status.PutString("stack", stack);
            }
            SendStatus((Result)statusCode, status);
        }
    }
}
