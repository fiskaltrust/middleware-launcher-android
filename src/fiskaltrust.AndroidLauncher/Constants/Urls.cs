namespace fiskaltrust.AndroidLauncher.Constants
{
    public static class Urls
    {
        public const string HELIPAD_SANDBOX = "https://helipad-sandbox.fiskaltrust.cloud/";
        public const string HELIPAD_PRODUCTION = "https://helipad.fiskaltrust.cloud/";
        
        public const string POSSYSTEM_API_SANDBOX = "https://possystem-api-sandbox.fiskaltrust.eu/";
        public const string POSSYSTEM_API_PRODUCTION = "https://possystem-api.fiskaltrust.eu/";

        // PL signing service, used when no SCU is hosted in the app. The sandbox serves a fake register.
        public const string SIGNING_PL_SANDBOX = "https://signing-sandbox.fiskaltrust.pl/sandbox/v2/";
        public const string SIGNING_PL_PRODUCTION = "https://signing.fiskaltrust.pl/v2/";
    }
}