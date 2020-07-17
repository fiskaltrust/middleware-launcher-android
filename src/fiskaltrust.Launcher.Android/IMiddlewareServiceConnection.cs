using Android.Content;

namespace fiskaltrust.AndroidLauncher
{
    public interface IMiddlewareServiceConnection : IServiceConnection
    {
        void OnManualDisconnect();
    }
}