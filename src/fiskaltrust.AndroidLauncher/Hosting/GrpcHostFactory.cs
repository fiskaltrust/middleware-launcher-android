using fiskaltrust.AndroidLauncher;
using fiskaltrust.AndroidLauncher.Hosting;
using fiskaltrust.ifPOS.v1;
using Grpc.Core;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.AndroidLauncher.Grpc.Hosting
{
    public class GrpcHostFactory : IHostFactory
    {
        public IHost<IPOS> CreatePosHost() => new GrpcPosHost();
    }

    public class GrpcPosHost : IHost<IPOS>, IDisposable
    {
        private Server _host;

        public Task StartAsync(string url, IPOS instance, LogLevel logLevel)
        {
            if (_host != null)
            {
                _host.ShutdownAsync().Wait();
            }

            _host = GrpcHelper.StartHost(url, instance);

            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            if (_host != null)
            {
                await _host.ShutdownAsync();
                _host = null;
            }
        }

        public void Dispose()
        {
            StopAsync().Wait();
        }
    }
}