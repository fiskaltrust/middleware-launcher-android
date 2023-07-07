using fiskaltrust.AndroidLauncher.Common.Enums;
using fiskaltrust.Middleware.Abstractions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;

namespace fiskaltrust.AndroidLauncher.Common.Hosting
{
    public interface IHost<T>
    {
        Task StartAsync(string url, T instance, LogLevel logLevel);
        Task StopAsync();
        Task<T> GetProxyAsync();
        IClientFactory<T> GetClientFactory();
    }

    public class ScuHost
    {
        private readonly object _host;
        public Type Interface { get; private set; }

        private ScuHost(Type interf, object host)
        {
            _host = host;
            Interface = interf;
        }

        public static ScuHost FromHost<T>(IHost<T> host)
        {
            return new ScuHost(typeof(T), host);
        }

        public IHost<T>? GetHost<T>()
        {
            if(Interface == typeof(T))
            {
                return (IHost<T>)_host;
            }
            return null;
        }
    }
}