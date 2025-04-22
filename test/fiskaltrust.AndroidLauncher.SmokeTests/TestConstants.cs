using System;

namespace fiskaltrust.AndroidLauncher.SmokeTests
{
    public static class TestConstants
    {
        public static class Http
        {
            public static readonly string CashboxId = Environment.GetEnvironmentVariable("CASHBOXID") ?? "e47c9e20-ebcc-4095-b24a-e449aab8f136";
            public static readonly string AccessToken = Environment.GetEnvironmentVariable("ACCESSTOKEN") ?? "BGmZghVi13+aZ/zP+ktJKk+Gp2A0rE5ut+2T6l9EykyqGN3NFCVa8B9le3LAoNmC1lcPAxro1Xe4bHF+UoWYr50=";
            public static readonly string Url = Environment.GetEnvironmentVariable("URL")?.Replace("rest://", "http://") ?? "http://localhost:1500/c3a2c180-fa33-482a-bcdc-a4cb57515564";
        }

        public static class Grpc
        {
            public static readonly string CashboxId = Environment.GetEnvironmentVariable("CASHBOXID") ?? "caaf4461-86ee-463b-9635-bc3e5f3bb14e";
            public static readonly string AccessToken = Environment.GetEnvironmentVariable("ACCESSTOKEN") ?? "BL57RfwgaYc95x2AcYTS+IIV2zCIM4C6s1sq7I23lzf8Zan9oY7lrH+IaokYu5ZXnAAlS4RbPw+GOs4MNm5wgiM=";
            public static readonly string Url = Environment.GetEnvironmentVariable("URL")?.Replace("rest://", "http://") ?? "grpc://localhost:18005";
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
