using ImageInterpreter.Domain;

namespace ImageInterpreter.Tests;

public class InterpretationTests
{
    private static readonly Guid BicycleId = Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7");

    [Fact]
    public void Create_returns_the_bicycle_sample_in_the_received_order()
    {
        var bicycle = DetectedObject.Create(
            "bicycle",
            Confidence.Create(0.94d),
            "A red bicycle in the foreground.");
        var mainStreet = ExtractedTextSpan.Create("MAIN ST", Confidence.Create(0.81d));
        var model = ModelIdentity.Create("provider", "model");

        var interpretation = Interpretation.Create(
            BicycleId,
            "A red bicycle leaning against a brick wall.",
            new[] { bicycle },
            new[] { mainStreet },
            Confidence.Create(0.9d),
            model);

        Assert.Equal(BicycleId, interpretation.Id);
        Assert.Equal("A red bicycle leaning against a brick wall.", interpretation.Summary);
        Assert.Equal(0.9d, interpretation.Confidence.Value);
        Assert.Same(model, interpretation.Model);
        Assert.Equal("provider", interpretation.Model.Provider);
        Assert.Equal("model", interpretation.Model.Name);

        var detected = Assert.Single(interpretation.Objects);
        Assert.Same(bicycle, detected);
        Assert.Equal("bicycle", detected.Label);
        Assert.Equal(0.94d, detected.Confidence.Value);
        Assert.Equal("A red bicycle in the foreground.", detected.Description);

        var extracted = Assert.Single(interpretation.ExtractedText);
        Assert.Same(mainStreet, extracted);
        Assert.Equal("MAIN ST", extracted.Text);
        Assert.Equal(0.81d, extracted.Confidence.Value);
    }

    [Fact]
    public void Create_accepts_empty_lists()
    {
        var interpretation = Interpretation.Create(
            BicycleId,
            "A plain gray square.",
            Array.Empty<DetectedObject>(),
            Array.Empty<ExtractedTextSpan>(),
            Confidence.Create(0d),
            ModelIdentity.Create("provider", "model"));

        Assert.Empty(interpretation.Objects);
        Assert.Empty(interpretation.ExtractedText);
    }

    [Fact]
    public void Create_accepts_duplicate_labels()
    {
        var first = DetectedObject.Create("bicycle", Confidence.Create(0.4d), "first");
        var second = DetectedObject.Create("bicycle", Confidence.Create(0.6d), "second");

        var interpretation = Interpretation.Create(
            BicycleId,
            "Two bicycles.",
            new[] { first, second },
            Array.Empty<ExtractedTextSpan>(),
            Confidence.Create(0.5d),
            ModelIdentity.Create("provider", "model"));

        Assert.Equal(new[] { "bicycle", "bicycle" }, interpretation.Objects.Select(item => item.Label).ToArray());
        Assert.Equal(new[] { "first", "second" }, interpretation.Objects.Select(item => item.Description).ToArray());
    }

    [Fact]
    public void Create_rejects_a_whitespace_summary()
    {
        Assert.Throws<ArgumentException>(() => Create(summary: "   "));
    }

    [Fact]
    public void Create_rejects_a_null_summary()
    {
        Assert.Throws<ArgumentNullException>(() => Create(summary: null!));
    }

    [Fact]
    public void Create_preserves_spaces_around_the_summary()
    {
        var interpretation = Create(summary: "  A plain gray square.  ");

        Assert.Equal("  A plain gray square.  ", interpretation.Summary);
    }

    [Fact]
    public void Create_rejects_a_null_object_list()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Interpretation.Create(
                BicycleId,
                "A red bicycle leaning against a brick wall.",
                null!,
                Array.Empty<ExtractedTextSpan>(),
                Confidence.Create(0.9d),
                ModelIdentity.Create("provider", "model")));
    }

    [Fact]
    public void Create_rejects_a_null_extracted_text_list()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Interpretation.Create(
                BicycleId,
                "A red bicycle leaning against a brick wall.",
                Array.Empty<DetectedObject>(),
                null!,
                Confidence.Create(0.9d),
                ModelIdentity.Create("provider", "model")));
    }

    [Fact]
    public void Create_rejects_a_null_object_item()
    {
        DetectedObject[] objects =
        [
            DetectedObject.Create("bicycle", Confidence.Create(0.94d), "A red bicycle in the foreground."),
            null!,
        ];

        Assert.Throws<ArgumentNullException>(() =>
            Interpretation.Create(
                BicycleId,
                "A red bicycle leaning against a brick wall.",
                objects,
                Array.Empty<ExtractedTextSpan>(),
                Confidence.Create(0.9d),
                ModelIdentity.Create("provider", "model")));
    }

    [Fact]
    public void Create_rejects_a_null_extracted_text_item()
    {
        ExtractedTextSpan[] extractedText =
        [
            ExtractedTextSpan.Create("MAIN ST", Confidence.Create(0.81d)),
            null!,
        ];

        Assert.Throws<ArgumentNullException>(() =>
            Interpretation.Create(
                BicycleId,
                "A red bicycle leaning against a brick wall.",
                Array.Empty<DetectedObject>(),
                extractedText,
                Confidence.Create(0.9d),
                ModelIdentity.Create("provider", "model")));
    }

    [Fact]
    public void Create_rejects_null_confidence()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Interpretation.Create(
                BicycleId,
                "A red bicycle leaning against a brick wall.",
                Array.Empty<DetectedObject>(),
                Array.Empty<ExtractedTextSpan>(),
                null!,
                ModelIdentity.Create("provider", "model")));
    }

    [Fact]
    public void Create_rejects_a_null_model()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Interpretation.Create(
                BicycleId,
                "A red bicycle leaning against a brick wall.",
                Array.Empty<DetectedObject>(),
                Array.Empty<ExtractedTextSpan>(),
                Confidence.Create(0.9d),
                null!));
    }

    [Fact]
    public void Create_accepts_guid_empty()
    {
        var interpretation = Create(id: Guid.Empty);

        Assert.Equal(Guid.Empty, interpretation.Id);
    }

    [Fact]
    public void Create_copies_lists_so_later_mutation_is_ignored()
    {
        var objects = new List<DetectedObject>
        {
            DetectedObject.Create("bicycle", Confidence.Create(0.94d), "A red bicycle in the foreground."),
            DetectedObject.Create("wall", Confidence.Create(0.7d), "A brick wall."),
        };
        var extractedText = new List<ExtractedTextSpan>
        {
            ExtractedTextSpan.Create("MAIN ST", Confidence.Create(0.81d)),
            ExtractedTextSpan.Create("LANE", Confidence.Create(0.4d)),
        };

        var interpretation = Interpretation.Create(
            BicycleId,
            "A red bicycle leaning against a brick wall.",
            objects,
            extractedText,
            Confidence.Create(0.9d),
            ModelIdentity.Create("provider", "model"));

        objects.Reverse();
        objects.Add(DetectedObject.Create("car", Confidence.Create(0.2d), ""));
        extractedText.Clear();

        Assert.Equal(new[] { "bicycle", "wall" }, interpretation.Objects.Select(item => item.Label).ToArray());
        Assert.Equal(
            new[] { "A red bicycle in the foreground.", "A brick wall." },
            interpretation.Objects.Select(item => item.Description).ToArray());
        Assert.Equal(new[] { "MAIN ST", "LANE" }, interpretation.ExtractedText.Select(item => item.Text).ToArray());
    }

    private static Interpretation Create(
        Guid? id = null,
        string summary = "A red bicycle leaning against a brick wall.",
        IReadOnlyList<DetectedObject>? objects = null,
        IReadOnlyList<ExtractedTextSpan>? extractedText = null,
        Confidence? confidence = null,
        ModelIdentity? model = null)
    {
        return Interpretation.Create(
            id ?? BicycleId,
            summary,
            objects ?? Array.Empty<DetectedObject>(),
            extractedText ?? Array.Empty<ExtractedTextSpan>(),
            confidence ?? Confidence.Create(0.9d),
            model ?? ModelIdentity.Create("provider", "model"));
    }
}
