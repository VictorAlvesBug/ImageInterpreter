using System.Reflection;
using ImageInterpreter.Application;

namespace ImageInterpreter.Tests;

public class InterpretImageTests
{
    private static readonly byte[] Image = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Fact]
    public async Task ExecuteAsync_returns_success_for_a_valid_draft()
    {
        var vision = new FakeVisionInterpreter(new VisionPortResult.Success(Draft()));
        var useCase = new InterpretImage(vision);

        var result = await useCase.ExecuteAsync("image/png", Image, CancellationToken.None);

        var success = Assert.IsType<InterpretImageResult.Success>(result);
        var interpretation = success.Interpretation;
        Assert.NotEqual(Guid.Empty, interpretation.Id);
        Assert.Equal("  A red bicycle leaning against a brick wall.  ", interpretation.Summary);
        Assert.Equal(1d, interpretation.Confidence.Value);
        Assert.Equal("provider", interpretation.Model.Provider);
        Assert.Equal("model", interpretation.Model.Name);

        Assert.Equal(2, interpretation.Objects.Count);
        Assert.Equal("bicycle", interpretation.Objects[0].Label);
        Assert.Equal(0d, interpretation.Objects[0].Confidence.Value);
        Assert.Equal("A red bicycle in the foreground.", interpretation.Objects[0].Description);
        Assert.Equal("bicycle", interpretation.Objects[1].Label);
        Assert.Equal(1d, interpretation.Objects[1].Confidence.Value);
        Assert.Equal("", interpretation.Objects[1].Description);

        var extracted = Assert.Single(interpretation.ExtractedText);
        Assert.Equal("MAIN ST", extracted.Text);
        Assert.Equal(0d, extracted.Confidence.Value);

        Assert.Equal(1, vision.CallCount);
        Assert.Equal("image/png", vision.MediaType);
        Assert.Equal(Image, vision.Image);
    }

    [Fact]
    public async Task ExecuteAsync_assigns_a_different_id_on_each_execution()
    {
        var vision = new FakeVisionInterpreter(new VisionPortResult.Success(Draft()));
        var useCase = new InterpretImage(vision);

        var first = Assert.IsType<InterpretImageResult.Success>(
            await useCase.ExecuteAsync("image/png", Image, CancellationToken.None));
        var second = Assert.IsType<InterpretImageResult.Success>(
            await useCase.ExecuteAsync("image/png", Image, CancellationToken.None));

        Assert.NotEqual(first.Interpretation.Id, second.Interpretation.Id);
    }

    [Fact]
    public async Task ExecuteAsync_returns_uninterpretable_when_the_port_returns_uninterpretable()
    {
        var vision = new FakeVisionInterpreter(new VisionPortResult.Uninterpretable());
        var useCase = new InterpretImage(vision);

        var result = await useCase.ExecuteAsync("image/png", Image, CancellationToken.None);

        Assert.IsType<InterpretImageResult.Uninterpretable>(result);
        Assert.IsNotType<InterpretImageResult.Success>(result);
        Assert.Equal(1, vision.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_returns_unavailable_when_the_port_returns_unavailable()
    {
        var vision = new FakeVisionInterpreter(new VisionPortResult.Unavailable());
        var useCase = new InterpretImage(vision);

        var result = await useCase.ExecuteAsync("image/png", Image, CancellationToken.None);

        Assert.IsType<InterpretImageResult.Unavailable>(result);
    }

    [Fact]
    public async Task ExecuteAsync_returns_unavailable_when_the_port_times_out()
    {
        var vision = new FakeVisionInterpreter(new TimeoutException("provider upstream timed out"));
        var useCase = new InterpretImage(vision);

        var result = await useCase.ExecuteAsync("image/jpeg", Image, CancellationToken.None);

        Assert.IsType<InterpretImageResult.Unavailable>(result);
    }

    [Fact]
    public async Task ExecuteAsync_propagates_cancellation_from_the_port()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var exception = new OperationCanceledException(cancellation.Token);
        var vision = new FakeVisionInterpreter(exception);
        var useCase = new InterpretImage(vision);

        var thrown = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            useCase.ExecuteAsync("image/png", Image, cancellation.Token));

        Assert.Same(exception, thrown);
    }

    [Fact]
    public async Task ExecuteAsync_returns_unavailable_when_confidence_is_above_one()
    {
        var vision = new FakeVisionInterpreter(new VisionPortResult.Success(Draft(confidence: 1.1d)));
        var useCase = new InterpretImage(vision);

        var result = await useCase.ExecuteAsync("image/png", Image, CancellationToken.None);

        Assert.IsType<InterpretImageResult.Unavailable>(result);
    }

    [Fact]
    public async Task ExecuteAsync_returns_unavailable_when_summary_is_whitespace()
    {
        var vision = new FakeVisionInterpreter(new VisionPortResult.Success(Draft(summary: "   ")));
        var useCase = new InterpretImage(vision);

        var result = await useCase.ExecuteAsync("image/png", Image, CancellationToken.None);

        Assert.IsType<InterpretImageResult.Unavailable>(result);
    }

    [Fact]
    public async Task ExecuteAsync_returns_unavailable_when_label_is_whitespace()
    {
        VisionDraft.DetectedObjectDraft[] objects = [new("   ", 0.5d, "A red bicycle in the foreground.")];
        var vision = new FakeVisionInterpreter(new VisionPortResult.Success(Draft(objects: objects)));
        var useCase = new InterpretImage(vision);

        var result = await useCase.ExecuteAsync("image/png", Image, CancellationToken.None);

        Assert.IsType<InterpretImageResult.Unavailable>(result);
    }

    [Fact]
    public void Draft_has_no_id_and_failure_results_have_no_message()
    {
        var draftProperties = typeof(VisionDraft)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(property => property.Name);

        Assert.DoesNotContain(draftProperties, name => string.Equals(name, "Id", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(PublicProperties(typeof(InterpretImageResult.Uninterpretable)));
        Assert.Empty(PublicProperties(typeof(InterpretImageResult.Unavailable)));
    }

    [Fact]
    public void Application_assembly_does_not_reference_aspnetcore()
    {
        var names = typeof(InterpretImage).Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name);

        Assert.DoesNotContain(
            names,
            name => name is not null && name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    private static PropertyInfo[] PublicProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
    }

    private static VisionDraft Draft(
        string summary = "  A red bicycle leaning against a brick wall.  ",
        IReadOnlyList<VisionDraft.DetectedObjectDraft>? objects = null,
        IReadOnlyList<VisionDraft.ExtractedTextDraft>? extractedText = null,
        double confidence = 1d,
        string provider = "provider",
        string name = "model")
    {
        return new VisionDraft(
            summary,
            objects ??
            [
                new VisionDraft.DetectedObjectDraft("bicycle", 0d, "A red bicycle in the foreground."),
                new VisionDraft.DetectedObjectDraft("bicycle", 1d, ""),
            ],
            extractedText ??
            [
                new VisionDraft.ExtractedTextDraft("MAIN ST", 0d),
            ],
            confidence,
            provider,
            name);
    }
}
