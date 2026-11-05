using System.Reflection;
using ImageInterpreter.Domain;

namespace ImageInterpreter.Tests;

public class DomainAssemblyTests
{
    [Fact]
    public void Domain_assembly_does_not_reference_aspnetcore()
    {
        var names = typeof(Interpretation).Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name);

        Assert.DoesNotContain(
            names,
            name => name is not null && name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Interpretation_exposes_only_the_document_properties()
    {
        var properties = typeof(Interpretation)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .ToDictionary(property => property.Name, property => property.PropertyType);

        Assert.Equal(
            new[] { "Confidence", "ExtractedText", "Id", "Model", "Objects", "Summary" },
            properties.Keys.OrderBy(name => name, StringComparer.Ordinal).ToArray());
        Assert.Equal(typeof(Guid), properties["Id"]);
        Assert.Equal(typeof(string), properties["Summary"]);
        Assert.Equal(typeof(IReadOnlyList<DetectedObject>), properties["Objects"]);
        Assert.Equal(typeof(IReadOnlyList<ExtractedTextSpan>), properties["ExtractedText"]);
        Assert.Equal(typeof(Confidence), properties["Confidence"]);
        Assert.Equal(typeof(ModelIdentity), properties["Model"]);
    }
}
