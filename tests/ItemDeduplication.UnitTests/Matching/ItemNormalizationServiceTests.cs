using ItemDeduplication.Infrastructure.Matching;

namespace ItemDeduplication.UnitTests.Matching;

public class ItemNormalizationServiceTests
{
    private readonly ItemNormalizationService _service = new();

    [Theory]
    [InlineData("VACUUM 60 CM", "vacuum 60 cm")]
    [InlineData("Vacuum-60-CM", "vacuum 60 cm")]
    [InlineData("مكنسة ٦٠ سم", "مكنسة 60 سم")]
    [InlineData("مُكْنَسَة ٦٠ سم", "مكنسة 60 سم")]
    [InlineData("إبراهيم ١٢", "ابراهيم 12")]
    [InlineData("آلات ١٠", "الات 10")]
    // new behaviour
    [InlineData("Vacuum 60cm", "vacuum 60 cm")]
    [InlineData("Water 2.5L", "water 2.5 l")]
    [InlineData("ماء ٢٫٥ لتر", "ماء 2.5 لتر")]
    [InlineData("Table 60x40 cm", "table 60 40 cm")]
    [InlineData("Pipe PN10", "pipe 10 bar")]
    [InlineData("Valve PN-16 DN50", "valve 16 bar dn50")]
    [InlineData("جلبه بلاسيتك PPH لون رمادى قطر 160 مم 10 بار بجوان", "جلبه بلاسيتك pph لون رمادي قطر 160 مم 10 بار بجوان")]
    [InlineData("جلبه بلاسيتك PPH لون رمادى قطر 160 مم PN10 بجوان", "جلبه بلاسيتك pph لون رمادي قطر 160 مم 10 bar بجوان")]
    public void Normalize_ShouldReturnExpectedValue(string input, string expected)
    {
        var result = _service.Normalize(input);

        Assert.Equal(expected, result.NormalizedName);
    }

    [Fact]
    public void Normalize_ShouldPreserveOriginalName()
    {
        const string original = "مُكْنَسَة ٦٠ سم";

        var result = _service.Normalize(original);

        Assert.Equal(original, result.OriginalName);
    }

    [Fact]
    public void Normalize_ShouldExtractTokens()
    {
        var result = _service.Normalize("Vacuum 60 CM");

        Assert.Equal(new[] { "vacuum", "60", "cm" }, result.Tokens);
    }

    [Fact]
    public void Normalize_ShouldHandleNullOrEmptyInput()
    {
        var nullResult = _service.Normalize(null!);
        var emptyResult = _service.Normalize("");

        Assert.Empty(nullResult.NormalizedName);
        Assert.Empty(emptyResult.NormalizedName);
        Assert.Empty(nullResult.Tokens);
    }
}
