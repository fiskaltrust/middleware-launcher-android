using fiskaltrust.AndroidLauncher.Common;
using fiskaltrust.AndroidLauncher.Common.Hosting;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Interface.Client.Grpc;
using Grpc.Core;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using fiskaltrust.AndroidLauncher.Common.Services.SCU;
using fiskaltrust.ifPOS.v1.it;
using System.Collections.Generic;

namespace fiskaltrust.AndroidLauncher.Grpc.Hosting
{
    public class GrpcHostFactory : IHostFactory
    {
        public IHost<IPOS> CreatePosHost() => new GrpcPosHost();
    }

    public class GrpcPosHost : IHost<IPOS>, IDisposable
    {
        private Server _host;
        private string _url;

        public IClientFactory<IPOS> GetClientFactory() => new POSClientFactory();

        public async Task<IPOS> GetProxyAsync()
        {
            return await GrpcPosFactory.CreatePosAsync(new GrpcClientOptions
            {
                Url = new Uri(_url)
            });
        }

        public Task StartAsync(string url, IPOS instance, LogLevel logLevel)
        {
            _url = url;
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