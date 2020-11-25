using Android.Content;
using Android.OS;
using Android.Util;
using fiskaltrust.AndroidLauncher.Common.Services;
using fiskaltrust.ifPOS.v1;
using System;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Common.AndroidService
{
    public class MiddlewareServiceConnection : Java.Lang.Object, IMiddlewareServiceConnection, IPOSProvider
    {
        private static readonly string TAG = typeof(MiddlewareServiceConnection).FullName;
        private readonly Action _onBound;
        private readonly Action _onUnbound;

        public MiddlewareServiceConnection(Action onBound = null, Action onUnbound = null)
        {
            IsConnected = false;
            Binder = null;

            _onBound = onBound;
            _onUnbound = onUnbound;
        }

        public bool IsConnected { get; private set; }
        public POSProviderBinder Binder { get; private set; }

        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            Binder = service as POSProviderBinder;
            IsConnected = Binder != null;

            if (IsConnected)
            {
                _onBound?.Invoke();
            }
            else
            {
                _onUnbound?.Invoke();
            }
        }

        public void OnServiceDisconnected(ComponentName name)
        {
            Log.Debug(TAG, $"OnServiceDisconnected {name.ClassName}");
            OnManualDisconnect();
        }

        public async Task<IPOS> GetPOSAsync()
        {
            if (!IsConnected)
            {
                return null;
            }

            return await ((Binder?.Service.GetPOSAsync()) ?? Task.FromResult<IPOS>(null));
        }

        public void OnManualDisconnect()
        {
            IsConnected = false;
            Binder = null;
            _onUnbound?.Invoke();
        }

        public async Task StopAsync() => await Binder?.Service.StopAsync();
    }
}