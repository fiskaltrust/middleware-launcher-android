using System;

namespace fiskaltrust.AndroidLauncher.Helpers.Logging
{
    internal class NoopDisposable : IDisposable
    {
        public void Dispose() { }
    }
}