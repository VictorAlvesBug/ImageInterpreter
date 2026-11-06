using ImageInterpreter.Domain;

namespace ImageInterpreter.Application;

public sealed class InterpretImage
{
    private readonly IVisionInterpreter _vision;

    public InterpretImage(IVisionInterpreter vision)
    {
        ArgumentNullException.ThrowIfNull(vision);
        _vision = vision;
    }

    public async Task<InterpretImageResult> ExecuteAsync(
        string mediaType,
        ReadOnlyMemory<byte> image,
        CancellationToken cancellationToken)
    {
        VisionPortResult portResult;
        try
        {
            portResult = await _vision.InterpretAsync(mediaType, image, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return new InterpretImageResult.Unavailable();
        }

        return portResult switch
        {
            VisionPortResult.Success success => Build(success.Draft),
            VisionPortResult.Uninterpretable => new InterpretImageResult.Uninterpretable(),
            VisionPortResult.Unavailable => new InterpretImageResult.Unavailable(),
            _ => new InterpretImageResult.Unavailable(),
        };
    }

    private static InterpretImageResult Build(VisionDraft draft)
    {
        try
        {
            var objects = new DetectedObject[draft.Objects.Count];
            for (var index = 0; index < objects.Length; index++)
            {
                var item = draft.Objects[index];
                objects[index] = DetectedObject.Create(
                    item.Label,
                    Confidence.Create(item.Confidence),
                    item.Description);
            }

            var extractedText = new ExtractedTextSpan[draft.ExtractedText.Count];
            for (var index = 0; index < extractedText.Length; index++)
            {
                var item = draft.ExtractedText[index];
                extractedText[index] = ExtractedTextSpan.Create(
                    item.Text,
                    Confidence.Create(item.Confidence));
            }

            var interpretation = Interpretation.Create(
                Guid.NewGuid(),
                draft.Summary,
                objects,
                extractedText,
                Confidence.Create(draft.Confidence),
                ModelIdentity.Create(draft.Provider, draft.Name));

            return new InterpretImageResult.Success(interpretation);
        }
        catch (ArgumentException)
        {
            return new InterpretImageResult.Unavailable();
        }
    }
}
