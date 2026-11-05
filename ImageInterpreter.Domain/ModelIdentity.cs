namespace ImageInterpreter.Domain;

public sealed record ModelIdentity
{
    public string Provider { get; }

    public string Name { get; }

    private ModelIdentity(string provider, string name)
    {
        Provider = provider;
        Name = name;
    }

    public static ModelIdentity Create(string provider, string name)
    {
        return new ModelIdentity(
            TextRules.RequireNonWhiteSpace(provider, nameof(provider)),
            TextRules.RequireNonWhiteSpace(name, nameof(name)));
    }
}
