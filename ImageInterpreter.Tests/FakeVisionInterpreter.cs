using ImageInterpreter.Application;

namespace ImageInterpreter.Tests;

public sealed class FakeVisionInterpreter : IVisionInterpreter
{
    private readonly VisionPortResult? _result;
    private readonly Exception? _exception;

    public FakeVisionInterpreter(VisionPortResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _result = result;
    }

    public FakeVisionInterpreter(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        _exception = exception;
    }

    public string? MediaType { get; private set; }

    public byte[]? Image { get; private set; }

    public int CallCount { get; private set; }

    public Task<VisionPortResult> InterpretAsync(
        string mediaType,
        ReadOnlyMemory<byte> image,
        CancellationToken cancellationToken)
    {
        CallCount++;
        MediaType = mediaType;
        Image = image.ToArray();

        if (_exception is not null)
        {
            throw _exception;
        }

        return Task.FromResult(_result!);
    }
}
