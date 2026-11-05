namespace ImageInterpreter.Domain;

public sealed record Confidence
{
    public double Value { get; }

    private Confidence(double value) => Value = value;

    public static Confidence Create(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d || value > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        return new Confidence(value);
    }
}
