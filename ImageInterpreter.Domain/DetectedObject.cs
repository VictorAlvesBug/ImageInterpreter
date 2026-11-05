namespace ImageInterpreter.Domain;

public sealed record DetectedObject
{
    public string Label { get; }

    public Confidence Confidence { get; }

    public string Description { get; }

    private DetectedObject(string label, Confidence confidence, string description)
    {
        Label = label;
        Confidence = confidence;
        Description = description;
    }

    public static DetectedObject Create(string label, Confidence confidence, string description)
    {
        ArgumentNullException.ThrowIfNull(confidence);

        return new DetectedObject(
            TextRules.RequireNonWhiteSpace(label, nameof(label)),
            confidence,
            TextRules.AllowEmpty(description, nameof(description)));
    }
}
