using ImageInterpreter.Domain;

namespace ImageInterpreter.Tests;

public class ConfidenceTests
{
    [Fact]
    public void Create_accepts_zero()
    {
        var confidence = Confidence.Create(0d);

        Assert.Equal(0d, confidence.Value);
    }

    [Fact]
    public void Create_accepts_one()
    {
        var confidence = Confidence.Create(1d);

        Assert.Equal(1d, confidence.Value);
    }

    [Fact]
    public void Create_accepts_an_intermediate_value()
    {
        var confidence = Confidence.Create(0.5d);

        Assert.Equal(0.5d, confidence.Value);
    }

    [Fact]
    public void Create_accepts_negative_zero()
    {
        var confidence = Confidence.Create(-0.0d);

        Assert.Equal(0d, confidence.Value);
    }

    [Fact]
    public void Create_rejects_a_value_below_zero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Confidence.Create(-0.1d));
    }

    [Fact]
    public void Create_rejects_a_value_above_one()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Confidence.Create(1.1d));
    }

    [Fact]
    public void Create_rejects_nan()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Confidence.Create(double.NaN));
    }

    [Fact]
    public void Create_rejects_positive_infinity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Confidence.Create(double.PositiveInfinity));
    }

    [Fact]
    public void Create_rejects_negative_infinity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Confidence.Create(double.NegativeInfinity));
    }
}
