namespace fiskaltrust.AndroidLauncher.SmokeTests
{
    public static class TestConstants
    {
        public static class Http
        {
            public const string CashboxId = "c7bdfec2-1c99-48d6-9ce0-5caaf613cd0b";
            public const string AccessToken = "BBzMuxESso0z6h7Od/imq8wLCYYO2jkDfGVpd2q+9BbC/GttKt1Iqj0u7uE8LOpd74EnKYSZMg6Dim0ZeK2Yi+4=";
            public const string Url = "http://localhost:1910/7ad0b5ee-56fb-42b7-89e4-1828b1450f68";
        }

        public static class Grpc
        {
            public const string CashboxId = "b957c72b-3321-4024-96bd-02f5c633623d";
            public const string AccessToken = "BD2Oyo2HOvOArjkcHYYlMEbl0rgmch4WzGFoSL4ZMlAXX4PVm5lc4qoUXsiLKou0/IdrDHIkh0rtYNPk+LMvDlU=";
            public const string Url = "grpc://localhost:1400";
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
