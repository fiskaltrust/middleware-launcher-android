using Android.Content;

namespace fiskaltrust.AndroidLauncher.Common.AndroidService
{
    public interface IMiddlewareServiceConnection : IServiceConnection
    {
        void OnManualDisconnect();
    }
}
