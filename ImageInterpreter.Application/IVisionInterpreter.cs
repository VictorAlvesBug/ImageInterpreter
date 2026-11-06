namespace ImageInterpreter.Application;

public interface IVisionInterpreter
{
    Task<VisionPortResult> InterpretAsync(
        string mediaType,
        ReadOnlyMemory<byte> image,
        CancellationToken cancellationToken);
}
