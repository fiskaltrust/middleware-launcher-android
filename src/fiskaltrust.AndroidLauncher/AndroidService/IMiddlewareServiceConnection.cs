using Android.Content;

namespace fiskaltrust.AndroidLauncher.AndroidService
{
    public interface IMiddlewareServiceConnection : IServiceConnection
    {
        void OnManualDisconnect();
    }
}
