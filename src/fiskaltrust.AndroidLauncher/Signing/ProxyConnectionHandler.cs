using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Signing
{
    internal class ProxyConnectionHandler<T>
    {
        private readonly T _proxy;

        public ProxyConnectionHandler(T proxy)
        {
            _proxy = proxy;
        }

        public Task ForceReconnectAsync() => Task.CompletedTask;

        public Task ReconnectAsync() => Task.CompletedTask;

        public async Task<T> GetProxyAsync() => await Task.FromResult(_proxy);
    }
}