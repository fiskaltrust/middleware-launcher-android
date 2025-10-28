using fiskaltrust.Api.POS.Helpers;
using fiskaltrust.Api.PosSystemLocal.Models;

namespace fiskaltrust.Api.PosSystemLocal.OperationHandling.Interface;

public interface IOperationProcessor<TRequest, TResponse>
{
    Task<Result<TResponse, ProblemDetails>> ProcessAsync(TRequest request, OperationProcessorOptions options);
}
