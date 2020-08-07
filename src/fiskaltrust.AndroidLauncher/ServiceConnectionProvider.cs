using fiskaltrust.AndroidLauncher.Common;
using System;

namespace fiskaltrust.AndroidLauncher
{
    internal class ServiceConnectionProvider
    {
        private static readonly Lazy<IMiddlewareServiceConnection> _lazy = new Lazy<IMiddlewareServiceConnection>(() => new MiddlewareServiceConnection());

        public static IMiddlewareServiceConnection GetConnection() => _lazy.Value;
    }
}