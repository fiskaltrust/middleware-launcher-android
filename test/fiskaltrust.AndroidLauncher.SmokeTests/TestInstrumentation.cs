using System.Diagnostics;
using System.Globalization;
using System.Xml.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;

namespace fiskaltrust.AndroidLauncher.SmokeTests
{
    [Instrumentation(
        Name = "eu.fiskaltrust.androidlauncher.smoketests.TestInstrumentation",
        TargetPackage = "eu.fiskaltrust.androidlauncher.smoketests")]
    public class TestInstrumentation : Instrumentation
    {
        private sealed record TestResult(string Name, TimeSpan Duration, Exception? Error);

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

            var results = new List<TestResult>();
            for (var i = 0; i < tests.Count; i++)
            {
                var test = tests[i];
                var current = i + 1;
                ReportStatus(StatusStart, test.Name, tests.Count, current, stream: $"{test.Name}: ");

                var stopwatch = Stopwatch.StartNew();
                try
                {
                    Log.Info(TAG, $"Running {test.Name}");
                    test.Run();
                    stopwatch.Stop();
                    Log.Info(TAG, $"{test.Name} passed");
                    results.Add(new TestResult(test.Name, stopwatch.Elapsed, Error: null));
                    ReportStatus(StatusOk, test.Name, tests.Count, current, stream: "OK\n");
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    Log.Warn(TAG, $"{test.Name} failed: {ex}");
                    results.Add(new TestResult(test.Name, stopwatch.Elapsed, ex));
                    ReportStatus(StatusFailure, test.Name, tests.Count, current, stream: $"FAILED\n{ex.Message}\n", stack: ex.ToString());
                }
            }

            try
            {
                WriteJUnitReport(results);
            }
            catch (Exception ex)
            {
                Log.Warn(TAG, $"Failed to write JUnit report: {ex}");
            }

            var failures = results.Count(r => r.Error != null);
            var summary = failures > 0
                ? $"\nFAILURES: {failures} of {tests.Count} tests failed\n"
                : $"\nOK: {tests.Count} tests passed\n";
            var resultBundle = new Bundle();
            resultBundle.PutString("stream", summary);
            Finish(failures == 0 && tests.Count > 0 ? Result.Ok : Result.Canceled, resultBundle);
        }

        /// <summary>
        /// Writes a JUnit XML report to the app's internal files dir, from where CI
        /// pulls it via `adb shell run-as ... cat files/test-results.xml`.
        /// </summary>
        private void WriteJUnitReport(List<TestResult> results)
        {
            var doc = new XDocument(
                new XElement("testsuite",
                    new XAttribute("name", "fiskaltrust.AndroidLauncher.SmokeTests"),
                    new XAttribute("tests", results.Count),
                    new XAttribute("failures", results.Count(r => r.Error != null)),
                    new XAttribute("time", results.Sum(r => r.Duration.TotalSeconds).ToString("F3", CultureInfo.InvariantCulture)),
                    new XAttribute("timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)),
                    results.Select(r =>
                    {
                        var testcase = new XElement("testcase",
                            new XAttribute("classname", nameof(SmokeTestSuite)),
                            new XAttribute("name", r.Name),
                            new XAttribute("time", r.Duration.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture)));
                        if (r.Error != null)
                        {
                            testcase.Add(new XElement("failure",
                                new XAttribute("message", r.Error.Message),
                                r.Error.ToString()));
                        }
                        return testcase;
                    })));

            var path = Path.Combine(TargetContext!.FilesDir!.AbsolutePath, "test-results.xml");
            doc.Save(path);
            Log.Info(TAG, $"JUnit report written to {path}");
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
