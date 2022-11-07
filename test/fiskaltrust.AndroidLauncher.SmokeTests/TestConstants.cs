using System;

namespace fiskaltrust.AndroidLauncher.SmokeTests
{
    public static class TestConstants
    {
        public static class Http
        {
            public static readonly string CashboxId = Environment.GetEnvironmentVariable("CASHBOXID") ?? "e02e85a4-dc4b-448f-9c30-3e1e9da71abf";
            public static readonly string AccessToken = Environment.GetEnvironmentVariable("ACCESSTOKEN") ?? "BH0MPsyWk3uqFy6fL3W2fPCRmjhXJrobOYSL9reW7c44kR7ycBN3G1vWmc1bdUbh4grU/c6CsQzYGcMH6RTJPr8=";
            public static readonly string Url = Environment.GetEnvironmentVariable("URL")?.Replace("rest://", "http://") ?? "http://localhost:1500/dec70a9c-4bb4-47e3-9686-78995b2c9f4e";
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
