using ImageInterpreter.Domain;

namespace ImageInterpreter.Tests;

public class ExtractedTextSpanTests
{
    [Fact]
    public void Create_rejects_whitespace_text()
    {
        Assert.Throws<ArgumentException>(() =>
            ExtractedTextSpan.Create("   ", Confidence.Create(0.81d)));
    }

    [Fact]
    public void Create_rejects_null_text()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ExtractedTextSpan.Create(null!, Confidence.Create(0.81d)));
    }

    [Fact]
    public void Create_rejects_null_confidence()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ExtractedTextSpan.Create("MAIN ST", null!));
    }

    [Fact]
    public void Create_preserves_spaces_around_text()
    {
        var span = ExtractedTextSpan.Create(" MAIN ST ", Confidence.Create(0.81d));

        Assert.Equal(" MAIN ST ", span.Text);
        Assert.Equal(0.81d, span.Confidence.Value);
    }
}
