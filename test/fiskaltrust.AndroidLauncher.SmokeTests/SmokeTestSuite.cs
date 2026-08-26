using Android.Content;

namespace fiskaltrust.AndroidLauncher.SmokeTests
{
    internal sealed record SmokeTest(string Name, Action Run);

    /// <summary>
    /// The smoke test suite, run by <see cref="TestInstrumentation"/>, which initializes
    /// <see cref="TargetContext"/>, <see cref="CashboxId"/>, and <see cref="AccessToken"/>
    /// before running the tests. Tests signal failure by throwing; any exception is
    /// reported as a test failure.
    /// </summary>
    internal static class SmokeTestSuite
    {
        public static Context TargetContext { get; set; } = null!;
        public static string CashboxId { get; set; } = TestConstants.DefaultCashboxId;
        public static string AccessToken { get; set; } = TestConstants.DefaultAccessToken;

        public static readonly IReadOnlyList<SmokeTest> Tests = new[]
        {
            new SmokeTest("Echo_ShouldReturn201_WhenSentViaPosSystemAPIIntent", () =>
            {
                var statusCode = ActivityTestActivity.SendRequest(
                    TargetContext,
                    PosSystemApiRequest.Create("POST", "/v2/echo", Headers, System.Text.Json.JsonSerializer.Serialize(new { Message = "Ping" }))
                );
                statusCode.ShouldBe("201");
            }),

            new SmokeTest("Sign_ShouldReturn201_WhenInitialOperationReceiptSentViaPosSystemAPIIntent", () =>
            {
                var statusCode = ActivityTestActivity.SendRequest(
                    TargetContext,
                    PosSystemApiRequest.Create("POST", "/v2/sign", Headers, TestConstants.InitialOperationReceipt.Replace("{{cashbox_id}}", CashboxId))
                );
                statusCode.ShouldBe("201");
            }),

            new SmokeTest("Echo_ShouldReturn201_WhenSentViaBoundService", () =>
            {
                var statusCode = ServiceTestActivity.SendRequest(
                    TargetContext,
                    PosSystemApiRequest.Create("POST", "/v2/echo", Headers, System.Text.Json.JsonSerializer.Serialize(new { Message = "Ping" }))
                );
                statusCode.ShouldBe("201");
            }),

            new SmokeTest("Sign_ShouldReturn201_WhenInitialOperationReceiptSentViaBoundService", () =>
            {

                var statusCode = ServiceTestActivity.SendRequest(
                    TargetContext,
                    PosSystemApiRequest.Create("POST", "/v2/sign", Headers, TestConstants.InitialOperationReceipt.Replace("{{cashbox_id}}", CashboxId))
                );
                statusCode.ShouldBe("201");
            }),
        };

        public static Dictionary<string, string> Headers => new()
        {
            { "Content-Type", "application/json" },
            { "x-cashbox-id", CashboxId },
            { "x-cashbox-accesstoken", AccessToken },
            { "x-operation-id", Guid.NewGuid().ToString() }
        };

        private static void ShouldBe(this string actual, string expected)
        {
            if (actual != expected)
            {
                throw new Exception($"Expected StatusCode {expected} but got {actual}");
            }
        }
    }
}
