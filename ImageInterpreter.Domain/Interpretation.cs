namespace ImageInterpreter.Domain;

public sealed record Interpretation
{
    public Guid Id { get; }

    public string Summary { get; }

    public IReadOnlyList<DetectedObject> Objects { get; }

    public IReadOnlyList<ExtractedTextSpan> ExtractedText { get; }

    public Confidence Confidence { get; }

    public ModelIdentity Model { get; }

    private Interpretation(
        Guid id,
        string summary,
        IReadOnlyList<DetectedObject> objects,
        IReadOnlyList<ExtractedTextSpan> extractedText,
        Confidence confidence,
        ModelIdentity model)
    {
        Id = id;
        Summary = summary;
        Objects = objects;
        ExtractedText = extractedText;
        Confidence = confidence;
        Model = model;
    }

    public static Interpretation Create(
        Guid id,
        string summary,
        IReadOnlyList<DetectedObject> objects,
        IReadOnlyList<ExtractedTextSpan> extractedText,
        Confidence confidence,
        ModelIdentity model)
    {
        ArgumentNullException.ThrowIfNull(confidence);
        ArgumentNullException.ThrowIfNull(model);

        return new Interpretation(
            id,
            TextRules.RequireNonWhiteSpace(summary, nameof(summary)),
            Copy(objects, nameof(objects)),
            Copy(extractedText, nameof(extractedText)),
            confidence,
            model);
    }

    private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T>? items, string paramName)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(items, paramName);

        var copy = new T[items.Count];
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            if (item is null)
            {
                throw new ArgumentNullException(paramName);
            }

            copy[index] = item;
        }

        return copy;
    }
}
