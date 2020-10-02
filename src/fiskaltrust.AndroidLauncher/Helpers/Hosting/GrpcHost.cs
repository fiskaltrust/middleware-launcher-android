using Grpc.Core;
using System;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Helpers.Hosting
{
    public class GrpcHost : IDisposable
    {
        private Server _host;

        public void StartService<T>(string url, T service) where T : class
        {
            if (_host != null)
            {
                _host.ShutdownAsync().RunSynchronously();
            }

            _host = GrpcHelper.StartHost(url, service);
        }

        public async Task ShutdownAsync()
        {
            await _host?.ShutdownAsync();
            _host = null;
        }

        public void Dispose()
        {
            ShutdownAsync().Wait();
        }
    }
}
