using fiskaltrust.Api.POS.Helpers;
using fiskaltrust.Api.PosSystemLocal.Models;

namespace fiskaltrust.Api.PosSystemLocal.OperationHandling.Interface;


public interface IRequestValidator<TRequest>
{
    Result<TRequest, ValidationError> Validate(string request);
}
