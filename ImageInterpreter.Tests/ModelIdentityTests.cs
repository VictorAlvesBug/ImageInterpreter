using ImageInterpreter.Domain;

namespace ImageInterpreter.Tests;

public class ModelIdentityTests
{
    [Fact]
    public void Create_rejects_a_whitespace_provider()
    {
        Assert.Throws<ArgumentException>(() => ModelIdentity.Create("   ", "model"));
    }

    [Fact]
    public void Create_rejects_a_whitespace_name()
    {
        Assert.Throws<ArgumentException>(() => ModelIdentity.Create("provider", "   "));
    }

    [Fact]
    public void Create_rejects_a_null_provider()
    {
        Assert.Throws<ArgumentNullException>(() => ModelIdentity.Create(null!, "model"));
    }

    [Fact]
    public void Create_rejects_a_null_name()
    {
        Assert.Throws<ArgumentNullException>(() => ModelIdentity.Create("provider", null!));
    }

    [Fact]
    public void Create_preserves_spaces_around_provider_and_name()
    {
        var model = ModelIdentity.Create(" provider ", " model ");

        Assert.Equal(" provider ", model.Provider);
        Assert.Equal(" model ", model.Name);
    }
}
