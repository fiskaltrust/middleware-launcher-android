using fiskaltrust.AndroidLauncher.Common.Enums;
using fiskaltrust.Middleware.Abstractions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace fiskaltrust.AndroidLauncher.Common.Hosting
{
    public interface IHost<T>
    {
        Task StartAsync(string url, T instance, LogLevel logLevel);
        Task StopAsync();
        Task<T> GetProxyAsync();
        IClientFactory<T> GetClientFactory();
    }
}