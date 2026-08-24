namespace fiskaltrust.AndroidLauncher.SmokeTests
{
    internal static class TestConstants
    {
        public const string DefaultCashboxId = "486fedc5-d200-465c-9b68-0c26dd6c0f72";
        public const string DefaultAccessToken = "BMcBfKxQbmBaL7ydBuyhqwz5FwO+yMvQsyfo6Vci/fcTkfjGn/13NvtpuvOHiLN5wr8/TGQia750708eTZoio3o=";

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
