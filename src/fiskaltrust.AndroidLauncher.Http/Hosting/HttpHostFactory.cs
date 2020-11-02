using fiskaltrust.AndroidLauncher.Common;
using fiskaltrust.AndroidLauncher.Common.Hosting;
using fiskaltrust.AndroidLauncher.Grpc.Controllers;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.ifPOS.v1.de;
using fiskaltrust.Middleware.Abstractions;
using fiskaltrust.Middleware.Interface.Client;
using fiskaltrust.Middleware.Interface.Client.Http;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Http.Hosting
{
    public class HttpHostFactory : IHostFactory
    {
        public IHost<IDESSCD> CreateDeSscdHost() => new HttpDeSscdHost();

        public IHost<IPOS> CreatePosHost() => new HttpPosHost();
    }

    public class HttpDeSscdHost : IHost<IDESSCD>, IDisposable
    {
        private IWebHost _host;
        private string _url;

        public IClientFactory<IDESSCD> GetClientFactory() => new DESSCDClientFactory();

        public async Task<IDESSCD> GetProxyAsync()
        {
            return await HttpDESSCDFactory.CreateSSCDAsync(new ClientOptions
            {
                Url = new Uri(_url)
            });
        }

        public async Task StartAsync(string url, IDESSCD instance)
        {
            if (_host != null)
            {
                await _host.StopAsync();
            }

            _url = url;
            var uri = new Uri(url);
            _host = WebHost
                .CreateDefaultBuilder()
                .UseKestrel()
                .ConfigureServices(services =>
                {
                    services.AddMvc(options =>
                    {
                        options.Conventions.Add(new IncludeControllerConvention<DESSCDController>());
                    });
                    services.AddSingleton<IDESSCD>(instance);
                })
                .Configure(app =>
                {
                    if (uri.Segments.Length > 1)
                        app.UsePathBase(new PathString(uri.AbsolutePath));
                    app.UseMvc();
                })
                .UseUrls(uri.GetLeftPart(UriPartial.Authority))
                .Build();

            await _host.StartAsync();
        }

        public async Task StopAsync()
        {
            if (_host != null)
            {
                await _host.StopAsync();
                _host = null;
            }
        }

        public void Dispose()
        {
            StopAsync().Wait();
        }
    }

    public class HttpPosHost : IHost<IPOS>, IDisposable
    {
        private IWebHost _host;
        private string _url;

        public IClientFactory<IPOS> GetClientFactory() => new POSClientFactory();

        public async Task<IPOS> GetProxyAsync()
        {
            return await HttpPosFactory.CreatePosAsync(new HttpPosClientOptions
            {
                Url = new Uri(_url)
            });
        }

        public async Task StartAsync(string url, IPOS instance)
        {
            if (_host != null)
            {
                await _host.StopAsync();
            }

            _url = url;
            var uri = new Uri(url);
            _host = WebHost
                .CreateDefaultBuilder()
                .UseKestrel()
                .ConfigureServices(services =>
                {
                    services.AddMvc(options =>
                    {
                        options.Conventions.Add(new IncludeControllerConvention<POSController>());
                    });
                    services.AddSingleton<IPOS>(instance);
                })
                .Configure(app =>
                {
                    if (uri.Segments.Length > 1)
                        app.UsePathBase(new PathString(uri.AbsolutePath));
                    app.UseMvc();
                })
                .UseUrls(uri.GetLeftPart(UriPartial.Authority))
                .Build();

            await _host.StartAsync();
        }

        public async Task StopAsync()
        {
            if (_host != null)
            {
                await _host.StopAsync();
                _host = null;
            }
        }

        public void Dispose()
        {
            StopAsync().Wait();
        }
    }
}