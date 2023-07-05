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

namespace fiskaltrust.AndroidLauncher.Grpc.Hosting
{
    public class GrpcHostFactory : IHostFactory
    {
        public IHost<SSCD> CreateSscdHost<T>() where T : SSCD => typeof(T) switch {
            Type t when t == typeof(DESSCD) => (IHost<SSCD>) new GrpcDeSscdHost(),
            Type t when t == typeof(ITSSCD) => (IHost<SSCD>) new GrpcItSscdHost()
            };

        public IHost<IPOS> CreatePosHost() => new GrpcPosHost();
    }

    public class GrpcDeSscdHost : IHost<DESSCD>, IDisposable
    {
        private Server _host;
        private string _url;

        public IClientFactory<DESSCD> GetClientFactory() => new DESSCDClientFactory();

        public async Task<DESSCD> GetProxyAsync()
        {
            return new DESSCD(await GrpcDESSCDFactory.CreateSSCDAsync(new GrpcClientOptions
            {
                Url = new Uri(_url)
            }));
        }

        public Task StartAsync(string url, DESSCD instance, LogLevel logLevel)
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

    public class GrpcItSscdHost : IHost<ITSSCD>, IDisposable
    {
        private Server _host;
        private string _url;

        public IClientFactory<ITSSCD> GetClientFactory() => new ITSSCDClientFactory();

        public async Task<ITSSCD> GetProxyAsync()
        {
            return new ITSSCD(await GrpcITSSCDFactory.CreateSSCDAsync(new GrpcClientOptions
            {
                Url = new Uri(_url)
            }));
        }

        public Task StartAsync(string url, ITSSCD instance, LogLevel logLevel)
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