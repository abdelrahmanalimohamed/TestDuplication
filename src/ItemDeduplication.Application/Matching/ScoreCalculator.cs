using ItemDeduplication.Application.Common.Models;
using Microsoft.Extensions.Options;

namespace ItemDeduplication.Application.Matching;

/// <summary>Combines the three signals into one confidence score (0..1).</summary>
public sealed class ScoreCalculator
{
    private readonly MatchingOptions _options;

    public ScoreCalculator(IOptions<MatchingOptions> options) => _options = options.Value;

    public double Combine(double? text, double? semantic, AttributeComparison attributes)
    {
        // Attributes alone can never prove two items are the same.
        if (text is null && semantic is null) return 0;

        var weightedSum = 0.0;
        var totalWeight = 0.0;

        void Add(double? score, double weight)
        {
            if (score is null) return;
            weightedSum += score.Value * weight;
            totalWeight += weight;
        }

        Add(text, _options.TextWeight);
        Add(semantic, _options.SemanticWeight);
        Add(attributes.Score, _options.AttributeWeight);

        var final = weightedSum / totalWeight;

        if (attributes.Conflicts > 0)
            final *= _options.ConflictPenalty;

        return Math.Clamp(final, 0, 1);
    }
}
