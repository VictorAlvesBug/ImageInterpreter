namespace ImageInterpreter.Application;

public abstract record VisionPortResult
{
    public sealed record Success(VisionDraft Draft) : VisionPortResult;

    public sealed record Uninterpretable : VisionPortResult;

    public sealed record Unavailable : VisionPortResult;
}
