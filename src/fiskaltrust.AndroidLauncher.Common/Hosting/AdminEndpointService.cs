using fiskaltrust.AndroidLauncher.Common.Helpers.Logging;
using fiskaltrust.AndroidLauncher.Common.Services;
using fiskaltrust.AndroidLauncher.Common.Services.Configuration;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;

namespace fiskaltrust.AndroidLauncher.Common.Hosting
{
    public class AdminEndpointService
    {
        private const string URL = "http://localhost:4654";

        private static readonly Lazy<AdminEndpointService> _lazyInstance = new Lazy<AdminEndpointService>(() => new AdminEndpointService());
        private IWebHost _host;

        public static AdminEndpointService Instance => _lazyInstance.Value;

        private AdminEndpointService() { }

        public async Task StartAsync()
        {
            if(_host != null)
            {
                await StopAsync();
            }

            var uri = new Uri(URL);
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
                            if (!request.Headers.ContainsKey("cashboxid") || !request.Headers.ContainsKey("accesstoken"))
                            {
                                response.StatusCode = StatusCodes.Status401Unauthorized;
                                await response.WriteAsync("The headers 'cashboxid' and 'accesstoken' are required to access this endpoint.");
                                return;
                            }

                            if(!await AuthenticateAsync(request.Headers["cashboxid"], request.Headers["accesstoken"]))
                            {
                                response.StatusCode = StatusCodes.Status403Forbidden;
                                return;
                            }

                            var fileToSend = FileLoggerHelper.LogDirectory.GetFiles("*.log").OrderByDescending(f=>f.LastWriteTime).FirstOrDefault();

                            if (fileToSend != null)
                            {
                                await response.SendFileAsync(GetIFileInfo(fileToSend.FullName));
                            }
                            else
                            {
                                response.StatusCode = StatusCodes.Status204NoContent;
                            }
                        });
                    });
                })
                .UseUrls(uri.GetLeftPart(UriPartial.Authority))
                .Build();

            await _host.StartAsync();
            
            Log.Logger.Information($"Admin endpoint is listening at '{URL}'.");
        }
        
        private IFileInfo GetIFileInfo(string fileName)
        {
            IFileProvider provider = new PhysicalFileProvider(AppDomain.CurrentDomain.BaseDirectory);
            IFileInfo fileInfo = provider.GetFileInfo(fileName);
            return fileInfo;
        }

        private async Task<bool> AuthenticateAsync(string cashboxid, string accesstoken)
        {
            var configProvider = new LocalConfigurationProvider();
            if (!Guid.TryParse(cashboxid, out var id))
                return false;

            return await configProvider.ConfigurationExistsAsync(id, accesstoken);
        }

        public async Task StopAsync() => await (_host?.StopAsync() ?? Task.CompletedTask);
    }
}