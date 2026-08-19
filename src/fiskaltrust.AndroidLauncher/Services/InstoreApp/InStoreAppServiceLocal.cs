using fiskaltrust.AndroidLauncher.Services.InStoreApp;
using fiskaltrust.Api.PosSystem.Core.Interfaces;
using fiskaltrust.Api.PosSystem.Core.Models;
using fiskaltrust.Api.PosSystem.Core.Payment.Hobex;
using fiskaltrust.Api.PosSystem.Core.v2.Pay.Models;
using fiskaltrust.Payment;
using Serilog;
using System.Collections.Concurrent;
using System.Text.Json;

namespace fiskaltrust.AndroidLauncher.Services.InStoreApp;

public sealed class InStoreAppServiceLocal : IInStoreAppService, IDisposable
{
    private readonly SemaphoreSlim _connectionGate = new(1, 1);
    private readonly InStoreAppClient _inStoreAppClient;
    private readonly ConcurrentDictionary<Guid, (TaskCompletionSource<PayRequestAcceptedResponse> acknowledge, TaskCompletionSource<PayResponseState> payResponseState)> _payRequests = new();
    private bool _disposed;

    public InStoreAppServiceLocal()
    {
        _inStoreAppClient = new InStoreAppClient();
        _inStoreAppClient.MessageReceived += OnMessageReceived;
    }

    public LauncherEnvironments InStoreAppEnvironment => LauncherEnvironments.Local;

    public async Task<(PayResponse? response, PaymentErrorResponse? errorResponse)> TriggerPaymentAsync(Guid operationId, Guid cashBoxId, ProcessorOptions options, PayRequest request)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.CashBoxAccessToken))
        {
            return (null, new PaymentErrorResponse
            {
                error = "A cashbox access token is required for local In Store App payments."
            });
        }

        var tcsAcknowledge = new TaskCompletionSource<PayRequestAcceptedResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var tcsPayResponse = new TaskCompletionSource<PayResponseState>(TaskCreationOptions.RunContinuationsAsynchronously);
        _payRequests[operationId] = (tcsAcknowledge, tcsPayResponse);

        try
        {
            await EnsureConnectedAsync().ConfigureAwait(false);
            await PublishPayRequestAsync(operationId, cashBoxId, options.CashBoxAccessToken, request).ConfigureAwait(false);
            return await AwaitPaymentResponseAsync(tcsPayResponse).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            return (null, new PaymentErrorResponse
            {
                error = "Failed to send payment request. Please check the connected devices."
            });
        }
        catch (Exception ex)
        {
            return (null, new PaymentErrorResponse
            {
                error = $"In Store App error: {ex.Message}"
            });
        }
        finally
        {
            _payRequests.TryRemove(operationId, out _);
        }
    }

    private static async Task<(PayResponse? response, PaymentErrorResponse? errorResponse)> AwaitPaymentResponseAsync(
        TaskCompletionSource<PayResponseState> payResponseSource)
    {
        var payResult = await payResponseSource.Task.ConfigureAwait(false);
        if (payResult.error != null)
        {
            return (null, new PaymentErrorResponse
            {
                error = "In Store App payment response failed: " + payResult.error
            });
        }

        return (payResult.PaymentResponse, null);
    }

    private async ValueTask<PayRequestAcceptedResponse?> PublishPayRequestAsync(Guid operationId, Guid cashBoxId, string accessToken, PayRequest payRequest)
    {
        var tcs = _payRequests[operationId].acknowledge;
        _inStoreAppClient.Send(InStoreAppMessages.MSG_PAY_REQUEST, operationId.ToString(), cashBoxId, accessToken, JsonSerializer.Serialize(payRequest));
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(10))).ConfigureAwait(false);
        if (completed != tcs.Task)
        {
            throw new TimeoutException("InStoreApp response timeout");
        }

        return await tcs.Task.ConfigureAwait(false);
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
            _inStoreAppClient.MessageReceived -= OnMessageReceived;
            _inStoreAppClient.Unbind(Android.App.Application.Context);
            _inStoreAppClient.Dispose();
            _connectionGate.Dispose();
        }
    }
    private async Task EnsureConnectedAsync()
    {
        await _connectionGate.WaitAsync().ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            if (!_inStoreAppClient.IsConnected)
            {
                _inStoreAppClient.Bind(Android.App.Application.Context);
                var isBound = await _inStoreAppClient.WaitForConnectionAsync().ConfigureAwait(false);
                if (!isBound)
                {
                    throw new Exception("InStore App Service is not running.");
                }
            }
        }
        finally
        {
            _connectionGate.Release();
        }
    }
    
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(InStoreAppServiceLocal));
        }
    }

    private void OnMessageReceived(InStoreAppEnvelope envelope)
    {
        try
        {
            if (envelope.What == InStoreAppMessages.MSG_PAY_RESPONSE_STATE)
            {
                var payResult = JsonSerializer.Deserialize<PayResponseState>(envelope.PayloadJson);
                if (payResult != null && (payResult.PaymentResponse != null || payResult.error != null))
                {
                    var operationId = Guid.Parse(payResult.operationId);
                    if (_payRequests.TryGetValue(operationId, out var payRequest))
                    {
                        payRequest.payResponseState.TrySetResult(payResult);
                    }
                }
            }
            else if (envelope.What == InStoreAppMessages.MSG_PAY_REQUEST_ACCEPTED)
            {
                var accepted = JsonSerializer.Deserialize<PayRequestAcceptedResponse>(envelope.PayloadJson);
                if (accepted != null)
                {
                    var operationId = Guid.Parse(accepted.operationId);
                    if (_payRequests.TryGetValue(operationId, out var payRequest))
                    {
                        payRequest.acknowledge.TrySetResult(accepted);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Warning("InStoreAppServiceLocal", $"Could not parse response payload: {ex.Message}");
        }
    }
}

