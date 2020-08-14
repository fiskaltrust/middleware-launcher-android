using Android.Content;

namespace fiskaltrust.AndroidLauncher.Common
{
    public interface IMiddlewareServiceConnection : IServiceConnection
    {
        void OnManualDisconnect();
    }
}
