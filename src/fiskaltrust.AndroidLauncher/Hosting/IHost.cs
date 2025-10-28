using fiskaltrust.AndroidLauncher.Enums;
using fiskaltrust.Middleware.Abstractions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;

namespace fiskaltrust.AndroidLauncher.Hosting
{
    public interface IHost<T>
    {
        Task StartAsync(string url, T instance, LogLevel logLevel);
        Task StopAsync();
    }
}