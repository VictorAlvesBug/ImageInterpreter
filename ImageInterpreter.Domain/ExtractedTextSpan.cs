namespace ImageInterpreter.Domain;

public sealed record ExtractedTextSpan
{
    public string Text { get; }

    public Confidence Confidence { get; }

    private ExtractedTextSpan(string text, Confidence confidence)
    {
        Text = text;
        Confidence = confidence;
    }

    public static ExtractedTextSpan Create(string text, Confidence confidence)
    {
        ArgumentNullException.ThrowIfNull(confidence);

        return new ExtractedTextSpan(
            TextRules.RequireNonWhiteSpace(text, nameof(text)),
            confidence);
    }
}
