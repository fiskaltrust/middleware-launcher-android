using System.Net;

namespace fiskaltrust.Api.PosSystemLocal.Models;

public class TypedResults<T>
{
    public HttpStatusCode HttpStatusCode { get; init; }
    public T? Value { get; init; }


    public static ProblemDetails Problem(int statusCode, string? title = null, string? detail = null)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        };
    }

    public static TypedResults<T> NotFound()
    {
        return new TypedResults<T>
        {
            HttpStatusCode = HttpStatusCode.NotFound,
            Value = default
        };
    }

    internal static ProblemDetails Failed(int statusCode, string data, string location)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = "Error",
            Detail = data
        };
    }

    internal static TypedResults<T> Done(T data, int statusCode, string location)
    {
        return new TypedResults<T>
        {
            HttpStatusCode = (HttpStatusCode)statusCode,
            Value = data,
        };
    }

    //public static ValidationProblemHttpResult ValidationProblem(IDictionary<string, string[]> errors)
    //{
    //    var validationProblemDetails = new ValidationProblemDetails(errors)
    //    {
    //        Status = StatusCodes.Status400BadRequest,
    //        Title = "One or more validation errors occurred."
    //    };
    //    return new ValidationProblemHttpResult(validationProblemDetails);
    //}
}

