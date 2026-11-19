using ItemDeduplication.Application.Common.Models;
using ItemDeduplication.Application.Matching;
using Microsoft.Extensions.Options;

namespace ItemDeduplication.UnitTests.Matching;

public class ScoreCalculatorTests
{
    private readonly ScoreCalculator _sut = new(Options.Create(new MatchingOptions()));

    [Fact]
    public void CrossLanguage_StrongSemanticAndMatchingAttributes_IsLikelyDuplicate()
    {
        var score = _sut.Combine(text: null, semantic: 0.8, new AttributeComparison(1, 0));

        Assert.True(score >= 0.85, $"score was {score}");
    }

    [Fact]
    public void AttributeConflict_PushesScoreBelowReviewThreshold()
    {
        var score = _sut.Combine(text: 1.0, semantic: 1.0, new AttributeComparison(0, 1));

        Assert.True(score < 0.65, $"score was {score}");
    }

    [Fact]
    public void ExactTextMatch_WithoutSemanticSignal_IsPerfect()
    {
        var score = _sut.Combine(text: 1.0, semantic: null, new AttributeComparison(0, 0));

        Assert.Equal(1.0, score);
    }

    [Fact]
    public void AttributesAlone_NeverProveDuplicate()
    {
        var score = _sut.Combine(text: null, semantic: null, new AttributeComparison(3, 0));

        Assert.Equal(0, score);
    }

    [Fact]
    public void ScriptHelper_DetectsCrossLanguage()
    {
        Assert.True(ScriptHelper.IsCrossLanguage("vacuum 60 cm", "مكنسة 60 سم"));
        Assert.False(ScriptHelper.IsCrossLanguage("vacuum 60 cm", "vacuum cleaner"));
    }
}
