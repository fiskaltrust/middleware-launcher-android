using System;

namespace fiskaltrust.AndroidLauncher.SmokeTests
{
    public static class TestConstants
    {
        public static class Http
        {
            public static readonly string CashboxId = Environment.GetEnvironmentVariable("CASHBOXID");
            public static readonly string AccessToken = Environment.GetEnvironmentVariable("ACCESSTOKEN");
            public static readonly string Url = Environment.GetEnvironmentVariable("URL").Replace("rest://", "http://");
        }

        public static class Grpc
        {
            public static readonly string CashboxId = Environment.GetEnvironmentVariable("CASHBOXID");
            public static readonly string AccessToken = Environment.GetEnvironmentVariable("ACCESSTOKEN");
            public static readonly string Url = Environment.GetEnvironmentVariable("URL");
        }

        public const string InitialOperationReceipt = @"
{
    ""ftCashBoxID"": ""{{cashbox_id}}"",
    ""cbTerminalID"": ""101"",
    ""cbReceiptReference"": ""INIT"",
    ""cbReceiptMoment"": ""2020-11-05T08:26:35Z"",
    ""cbChargeItems"": [],
    ""cbPayItems"": [],
    ""ftReceiptCase"": 4919338172267102211,
    ""cbUser"": ""Admin""
}";
    }
}
