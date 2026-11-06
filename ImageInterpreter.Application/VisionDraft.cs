namespace ImageInterpreter.Application;

public sealed record VisionDraft
{
    public string Summary { get; }

    public IReadOnlyList<DetectedObjectDraft> Objects { get; }

    public IReadOnlyList<ExtractedTextDraft> ExtractedText { get; }

    public double Confidence { get; }

    public string Provider { get; }

    public string Name { get; }

    public VisionDraft(
        string summary,
        IReadOnlyList<DetectedObjectDraft> objects,
        IReadOnlyList<ExtractedTextDraft> extractedText,
        double confidence,
        string provider,
        string name)
    {
        Summary = summary;
        Objects = objects;
        ExtractedText = extractedText;
        Confidence = confidence;
        Provider = provider;
        Name = name;
    }

    public sealed record DetectedObjectDraft(string Label, double Confidence, string Description);

    public sealed record ExtractedTextDraft(string Text, double Confidence);
}
