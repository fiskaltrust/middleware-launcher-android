using fiskaltrust.AndroidLauncher.Common.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace fiskaltrust.AndroidLauncher.Common.Hosting
{
    public class AdminEndpointService
    {
        private static readonly Lazy<AdminEndpointService> _lazyInstance = new Lazy<AdminEndpointService>(() => new AdminEndpointService());
        private IWebHost _host;

        public static AdminEndpointService Instance => _lazyInstance.Value;

        private AdminEndpointService() { }

        public async Task StartAsync()
        {
            var uri = new Uri("http://localhost:4654");
            _host = WebHost
                .CreateDefaultBuilder()
                .UseKestrel()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                })
                .Configure(app =>
                {
                    app.UseRouter(router =>
                    {
                        router.MapGet("fiskaltrust/state", async (request, response, routerData) =>
                        {
                            await response.WriteAsync(JsonConvert.SerializeObject(StateProvider.Instance.CurrentValue));
                        });
                        router.MapGet("fiskaltrust/logs", async (request, response, routerData) =>
                        {
                            //TODO check cashboxid & accesstoken headers and return logs
                        });
                    });
                })
                .UseUrls(uri.GetLeftPart(UriPartial.Authority))
                .Build();

            await _host.StartAsync();
        }

        public async Task StopAsync() => await (_host?.StopAsync() ?? Task.CompletedTask);
    }
}