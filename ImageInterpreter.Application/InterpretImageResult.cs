using ImageInterpreter.Domain;

namespace ImageInterpreter.Application;

public abstract record InterpretImageResult
{
    public sealed record Success(Interpretation Interpretation) : InterpretImageResult;

    public sealed record Uninterpretable : InterpretImageResult;

    public sealed record Unavailable : InterpretImageResult;
}
