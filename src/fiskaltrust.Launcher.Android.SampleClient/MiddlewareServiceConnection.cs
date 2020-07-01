using Android.Content;
using Android.OS;
using Android.Util;
using fiskaltrust.AndroidLauncher.Services;
using fiskaltrust.ifPOS.v1;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.SampleClient
{
    public class MiddlewareServiceConnection : Java.Lang.Object, IServiceConnection, IPOSProvider
    {
        private static readonly string TAG = typeof(MiddlewareServiceConnection).FullName;
        private readonly MainActivity _mainActivity;

        public MiddlewareServiceConnection(MainActivity activity)
        {
            IsConnected = false;
            Binder = null;
            _mainActivity = activity;
        }

        public bool IsConnected { get; private set; }
        public POSProviderBinder Binder { get; private set; }

        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            Binder = service as POSProviderBinder;
            IsConnected = Binder != null;

            if (IsConnected)
            {
                _mainActivity.UpdateUiForBoundService();
            }
            else
            {
                _mainActivity.UpdateUiForUnboundService();
            }
        }

        public void OnServiceDisconnected(ComponentName name)
        {
            Log.Debug(TAG, $"OnServiceDisconnected {name.ClassName}");
            IsConnected = false;
            Binder = null;
            _mainActivity.UpdateUiForUnboundService();
        }

        public Task<IPOS> GetPOSAsync()
        {
            if (!IsConnected)
            {
                return null;
            }

            return Binder?.Service.GetPOSAsync();
        }
    }
}