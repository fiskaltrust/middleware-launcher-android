using System;

namespace fiskaltrust.AndroidLauncher.Common.AndroidService
{
    public class ServiceConnectionProvider
    {
        private static readonly Lazy<MiddlewareServiceConnection> _lazy = new Lazy<MiddlewareServiceConnection>(() => new MiddlewareServiceConnection());

        public static MiddlewareServiceConnection GetConnection() => _lazy.Value;
    }
}