using System;

namespace fiskaltrust.AndroidLauncher.SmokeTests
{
    public static class TestConstants
    {
        public static class Http
        {
            public static readonly string CashboxId =  Environment.GetEnvironmentVariable("CASHBOXID") ?? "e02e85a4-dc4b-448f-9c30-3e1e9da71abf";
            public static readonly string AccessToken = Environment.GetEnvironmentVariable("ACCESSTOKEN") ?? "BH0MPsyWk3uqFy6fL3W2fPCRmjhXJrobOYSL9reW7c44kR7ycBN3G1vWmc1bdUbh4grU/c6CsQzYGcMH6RTJPr8=";
            public static readonly string Url = Environment.GetEnvironmentVariable("URL")?.Replace("rest://", "http://") ?? "http://localhost:1500/dec70a9c-4bb4-47e3-9686-78995b2c9f4e";
        }

        public static class Grpc
        {
            public static readonly string CashboxId = Environment.GetEnvironmentVariable("CASHBOXID") ?? "61385be4-7d8d-403b-8860-fbc5a07ed128";
            public static readonly string AccessToken = Environment.GetEnvironmentVariable("ACCESSTOKEN") ?? "BKmQdtsJvAmhT12xewUXW39XyzJZhePtUN//u4svDPLAVlEs2nRefmZ9iFH7dCARdQjB7XYG6yILXmsmSVMtheE=";
            public static readonly string Url = Environment.GetEnvironmentVariable("URL") ?? "grpc://localhost:1400";
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

        public const string Receipt = @"{
    ""ftCashBoxID"": ""{{cashbox_id}}"",
    ""cbTerminalID"": ""T2"",
    ""cbReceiptReference"": ""pos-action-identification-02"",
    ""cbReceiptMoment"": ""2020-11-05T08:26:35Z"",
    ""cbChargeItems"": [
        {
            ""Quantity"": 2.0,
            ""Description"": ""Coffee to Go"",
            ""Amount"": 2,
            ""VATRate"": 19.00,
            ""ftChargeItemCase"": 4919338167972134929,
            ""Moment"": ""2020-06-29T17:45:40.505Z""
        },
        {
            ""Quantity"": 1.0,
            ""Description"": ""Brötchen"",
            ""Amount"": 2.50,
            ""VATRate"": 7.00,
            ""ftChargeItemCase"": 4919338167972200466,
            ""Moment"": ""2020-06-29T17:45:40.505Z""
        }
    ],
    ""cbPayItems"": [
        {
            ""Quantity"": 1.0,
            ""Description"": ""Cash"",
            ""Amount"": 4.0,
            ""ftPayItemCase"": 4919338167972134913,
            ""Moment"": ""2020-06-29T18:05:33.912Z""
        }
    ],
    ""ftReceiptCase"": 4919338172267102209
}";
    }
}
