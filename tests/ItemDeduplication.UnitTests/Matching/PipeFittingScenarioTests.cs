using ItemDeduplication.Application.Matching;
using ItemDeduplication.Infrastructure.Matching;

namespace ItemDeduplication.UnitTests.Matching;

/// <summary>The real-world pair: "10 بار" and "PN10" describe the same pressure rating.</summary>
public class PipeFittingScenarioTests
{
    private const string Item1 = "جلبه بلاسيتك PPH لون رمادى قطر 160 مم 10 بار بجوان";
    private const string Item2 = "جلبه بلاسيتك PPH لون رمادى قطر 160 مم PN10 بجوان";

    private readonly ItemNormalizationService _normalizer = new();
    private readonly AttributeExtractor _attributes = new();
    private readonly FuzzyTextMatcher _text = new();

    [Fact]
    public void Attributes_DiameterAndPressure_Match()
    {
        var a = _attributes.Extract(_normalizer.Normalize(Item1).NormalizedName);
        var b = _attributes.Extract(_normalizer.Normalize(Item2).NormalizedName);

        var result = _attributes.Compare(a, b);

        Assert.Equal(2, result.Matches);   // length 160 mm + pressure 10 bar
        Assert.Equal(0, result.Conflicts);
    }

    [Fact]
    public void Attributes_DifferentPressure_IsConflict()
    {
        var pn16 = _normalizer.Normalize("جلبه بلاسيتك PPH قطر 160 مم PN16").NormalizedName;
        var bar10 = _normalizer.Normalize("جلبه بلاسيتك PPH قطر 160 مم 10 بار").NormalizedName;

        var result = _attributes.Compare(_attributes.Extract(pn16), _attributes.Extract(bar10));

        Assert.Equal(1, result.Matches);
        Assert.Equal(1, result.Conflicts);
    }

    [Fact]
    public void Text_ArabicIsNotStripped_AndSimilarPairScoresHigh()
    {
        var a = _normalizer.Normalize(Item1).NormalizedName;
        var b = _normalizer.Normalize(Item2).NormalizedName;

        var similar = _text.Score(a, b);
        var different = _text.Score("مكنسة كهربائية 60 سم", "مروحة سقف 120 سم");

        Assert.True(similar >= 0.85, $"similar was {similar}");
        Assert.True(different < 0.6, $"different was {different}");
    }
}
