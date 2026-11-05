using ImageInterpreter.Domain;

namespace ImageInterpreter.Tests;

public class DetectedObjectTests
{
    [Fact]
    public void Create_rejects_a_whitespace_label()
    {
        Assert.Throws<ArgumentException>(() =>
            DetectedObject.Create("   ", Confidence.Create(1d), "A red bicycle in the foreground."));
    }

    [Fact]
    public void Create_rejects_a_null_label()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DetectedObject.Create(null!, Confidence.Create(1d), "A red bicycle in the foreground."));
    }

    [Fact]
    public void Create_rejects_null_confidence()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DetectedObject.Create("bicycle", null!, "A red bicycle in the foreground."));
    }

    [Fact]
    public void Create_rejects_a_null_description()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DetectedObject.Create("bicycle", Confidence.Create(1d), null!));
    }

    [Fact]
    public void Create_accepts_an_empty_description()
    {
        var detected = DetectedObject.Create("bicycle", Confidence.Create(1d), "");

        Assert.Equal("", detected.Description);
    }

    [Fact]
    public void Create_accepts_a_whitespace_description()
    {
        var detected = DetectedObject.Create("bicycle", Confidence.Create(1d), "   ");

        Assert.Equal("   ", detected.Description);
    }

    [Fact]
    public void Create_preserves_spaces_around_the_label()
    {
        var detected = DetectedObject.Create(" bicycle ", Confidence.Create(0.94d), "A red bicycle in the foreground.");

        Assert.Equal(" bicycle ", detected.Label);
    }
}
