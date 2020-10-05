using System;

namespace fiskaltrust.AndroidLauncher.AndroidService
{
    internal class ServiceConnectionProvider
    {
        private static readonly Lazy<MiddlewareServiceConnection> _lazy = new Lazy<MiddlewareServiceConnection>(() => new MiddlewareServiceConnection());

        public static MiddlewareServiceConnection GetConnection() => _lazy.Value;
    }
}