using ItemDeduplication.Application.Matching;

namespace ItemDeduplication.UnitTests.Matching;

public class AttributeExtractorTests
{
    private readonly AttributeExtractor _sut = new();

    [Fact]
    public void Extract_EnglishAndArabicLength_ProduceSameBaseValue()
    {
        var en = _sut.Extract("vacuum 60 cm");
        var ar = _sut.Extract("مكنسة 60 سم");

        Assert.Single(en);
        Assert.Equal("length", en[0].Type);
        Assert.Equal(600m, en[0].Value);
        Assert.Equal(en[0], ar[0]);
    }

    [Fact]
    public void Compare_SameAttributesAcrossLanguages_Matches()
    {
        var result = _sut.Compare(_sut.Extract("vacuum 60 cm"), _sut.Extract("مكنسة 60 سم"));

        Assert.Equal(1, result.Matches);
        Assert.Equal(0, result.Conflicts);
        Assert.Equal(1.0, result.Score);
    }

    [Fact]
    public void Compare_DifferentSize_IsConflict()
    {
        var result = _sut.Compare(_sut.Extract("vacuum 60 cm"), _sut.Extract("vacuum 80 cm"));

        Assert.Equal(0, result.Matches);
        Assert.Equal(1, result.Conflicts);
    }

    [Fact]
    public void Compare_LitersAndMilliliters_Match()
    {
        var result = _sut.Compare(_sut.Extract("water 2.5 l"), _sut.Extract("ماء 2500 مل"));

        Assert.Equal(1, result.Matches);
        Assert.Equal(0, result.Conflicts);
    }

    [Fact]
    public void Compare_NoCommonAttributes_IsNeutral()
    {
        var result = _sut.Compare(_sut.Extract("vacuum cleaner"), _sut.Extract("vacuum 60 cm"));

        Assert.False(result.HasComparableData);
        Assert.Null(result.Score);
    }
}
