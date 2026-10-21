using fiskaltrust.Api.PosSystem.Core.InStoreApp;
using fiskaltrust.Api.PosSystem.Core.Interfaces;
using fiskaltrust.Api.PosSystem.Core.Models;
using fiskaltrust.Api.PosSystem.Core.Payment.Hobex;
using fiskaltrust.Api.PosSystem.Core.v2.Pay.Models;
using Microsoft.Extensions.Logging;
using MQTTnet.Extensions.ManagedClient;

namespace fiskaltrust.AndroidLauncher.Services.InStoreApp;

public class InStoreAppServiceRouter : IInStoreAppService, IDisposable
{
    private readonly InStoreAppServiceLocal _inStoreAppServiceLocal;
    private readonly InStoreAppService _inStoreAppServiceCloud;
    private bool _disposed;
    public InStoreAppServiceRouter(IManagedMqttClient managedMqttClient, ILoggerFactory loggerFactory)
    {
        _inStoreAppServiceLocal = new InStoreAppServiceLocal();
        _inStoreAppServiceCloud = new InStoreAppService(managedMqttClient,loggerFactory.CreateLogger<InStoreAppService>());
    }

    public async Task<(PayResponse? response, PaymentErrorResponse? errorResponse)> TriggerPaymentAsync(InStoreAppOptions inStoreAppOptions, PayRequest request)
    {
        if(await _inStoreAppServiceLocal.EnsureConnectedAsync().ConfigureAwait(false))
        {
            try
            {
                var localResponse = await _inStoreAppServiceLocal.TriggerPaymentAsync(inStoreAppOptions, request).ConfigureAwait(false);
                if (localResponse.response != null || localResponse.errorResponse != null)
                {
                    return localResponse;
                }
            }
            catch (InStoreAppPaymentNotAcceptedException)
            {

            }
        }
       
        return await _inStoreAppServiceCloud.TriggerPaymentAsync(inStoreAppOptions, request).ConfigureAwait(false);
       
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (disposing)
        {
           _inStoreAppServiceLocal.Dispose();
        }
    }

}
