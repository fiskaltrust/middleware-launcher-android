using System;

namespace fiskaltrust.AndroidLauncher.Common.Helpers.Logging
{
    internal class NoopDisposable : IDisposable
    {
        public void Dispose() { }
    }
}