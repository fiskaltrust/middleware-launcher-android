using System;

namespace fiskaltrust.AndroidLauncher.SmokeTests
{
    public static class TestConstants
    {
        public static class Http
        {
            public static readonly string CashboxId =  Environment.GetEnvironmentVariable("CASHBOXID") ?? "0a54122c-e247-4498-9e40-cc667c17e6aa";
            public static readonly string AccessToken = Environment.GetEnvironmentVariable("ACCESSTOKEN") ?? "BH0MPsyWk3uqFy6fL3W2fPCRmjhXJrobOYSL9reW7c44kR7ycBN3G1vWmc1bdUbh4grU/c6CsQzYGcMH6RTJPr8=";
            public static readonly string Url = Environment.GetEnvironmentVariable("URL") ?? "http://localhost:1500/dec70a9c-4bb4-47e3-9686-78995b2c9f4e";
        }

        public static class Grpc
        {
            public static readonly string CashboxId = Environment.GetEnvironmentVariable("CASHBOXID") ?? "61385be4-7d8d-403b-8860-fbc5a07ed128";
            public static readonly string AccessToken = Environment.GetEnvironmentVariable("ACCESSTOKEN") ?? "BKmQdtsJvAmhT12xewUXW39XyzJZhePtUN//u4svDPLAVlEs2nRefmZ9iFH7dCARdQjB7XYG6yILXmsmSVMtheE=";
            public static readonly string Url = "grpc://localhost:1200";
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
