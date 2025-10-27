namespace fiskaltrust.Api.PosSystemLocal.Models;

public record ValidationError
{
    public record Problem(ProblemDetails Error) : ValidationError();
    public static implicit operator ValidationError(ProblemDetails Error) => new Problem(Error);

    private ValidationError() { } // private constructor can prevent derived cases from being defined elsewhere
}
