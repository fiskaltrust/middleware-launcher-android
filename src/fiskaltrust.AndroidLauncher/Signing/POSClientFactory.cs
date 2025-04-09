using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Abstractions;
using System;
using System.Collections.Generic;


namespace fiskaltrust.AndroidLauncher.Signing
{
    public class POSClientFactory : IClientFactory<IPOS>
    {
        private readonly IPOS _queue;

        public POSClientFactory(IPOS queue)
        {
            _queue = queue;
        }

        public IPOS CreateClient(ClientConfiguration configuration)
        {
            return _queue;
        }
    }
}