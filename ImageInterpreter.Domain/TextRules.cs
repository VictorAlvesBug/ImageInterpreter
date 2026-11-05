namespace ImageInterpreter.Domain;

internal static class TextRules
{
    public static string RequireNonWhiteSpace(string? value, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
        return value;
    }

    public static string AllowEmpty(string? value, string paramName)
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
        return value;
    }
}
