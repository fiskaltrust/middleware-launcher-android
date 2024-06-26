﻿using fiskaltrust.AndroidLauncher.Common.Hosting;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using fiskaltrust.AndroidLauncher.Common.Extensions;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.AndroidLauncher.Http.Hosting
{
    public class HttpHost<T, TController> : IHost<T>, IDisposable
        where T : class
        where TController : Controller
    {
        protected string Url;
        private IWebHost _host;

        public async Task StartAsync(string url, T instance, LogLevel logLevel)
        {
            if (_host != null)
            {
                await _host.StopAsync();
            }

            Url = url;
            var uri = new Uri(url);
            _host = WebHost
                .CreateDefaultBuilder()
                .UseKestrel()
                .ConfigureServices(services =>
                {
                    services.AddLogProviders(logLevel);
                    services.AddMvc(options =>
                    {
                        options.Conventions.Add(new IncludeControllerConvention<TController>());
                    });
                    services.AddSingleton<T>(instance);
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