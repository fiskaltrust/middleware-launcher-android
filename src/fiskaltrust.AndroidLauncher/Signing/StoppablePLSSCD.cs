using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.pl;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Exceptions;
using fiskaltrust.Middleware.SCU.PL.Abstraction.Helpers;

namespace fiskaltrust.AndroidLauncher.Signing;

/// <summary>
/// Puts the SCU hosted in this app between the queue and its device, so the middleware can be stopped
/// without cutting off a fiscal operation. The POS System API serves requests concurrently and outside
/// the startup lock, so a restart can begin while a sale is still printing; disposing the SCU then would
/// close its device connection mid-receipt. <see cref="StopAsync"/> therefore refuses new calls and
/// waits for the running ones before the caller disposes the SCU.
/// </summary>
/// <remarks>
/// A refused receipt has not reached the register, so it is answered as such — the register could not
/// be reached, retry — and not with an unknown outcome. A running call is waited for without a time limit,
/// like the SCU itself waits for a register that holds a receipt.
/// </remarks>
public sealed class StoppablePLSSCD(IPLSSCD inner) : IPLSSCD
{
    private const string StoppingMessage = "The middleware is restarting, so nothing was sent to the fiscal register.";

    private readonly object _sync = new();
    private readonly TaskCompletionSource _drained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _running;
    private bool _stopping;

    public Task<EchoResponse> EchoAsync(EchoRequest request) => RunAsync(() => inner.EchoAsync(request));

    public Task<PLSSCDInfo> GetInfoAsync() => RunAsync(inner.GetInfoAsync);

    public async Task<ProcessResponse> ProcessReceiptAsync(ProcessRequest request)
    {
        if (!TryEnter())
        {
            request.ReceiptResponse.SetError(new PLDeviceUnreachableException(StoppingMessage));
            return new ProcessResponse { ReceiptResponse = request.ReceiptResponse };
        }
        try
        {
            return await inner.ProcessReceiptAsync(request);
        }
        finally
        {
            Leave();
        }
    }

    /// <summary>Refuses new calls and completes once no call is running any more.</summary>
    public Task StopAsync()
    {
        lock (_sync)
        {
            _stopping = true;
            if (_running == 0)
            {
                _drained.TrySetResult();
            }
        }
        return _drained.Task;
    }

    private async Task<T> RunAsync<T>(Func<Task<T>> call)
    {
        if (!TryEnter())
        {
            throw new PLDeviceUnreachableException(StoppingMessage);
        }
        try
        {
            return await call();
        }
        finally
        {
            Leave();
        }
    }

    private bool TryEnter()
    {
        lock (_sync)
        {
            if (_stopping)
            {
                return false;
            }
            _running++;
            return true;
        }
    }

    private void Leave()
    {
        lock (_sync)
        {
            _running--;
            if (_stopping && _running == 0)
            {
                _drained.TrySetResult();
            }
        }
    }
}
