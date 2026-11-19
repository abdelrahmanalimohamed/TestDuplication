namespace ItemDeduplication.Application.Common.Models;

/// <summary>A measurable attribute. Value is stored in a base unit (mm, g, ml, V, W, Hz, count).</summary>
public sealed record ItemAttribute(string Type, decimal Value);

public sealed record AttributeComparison(int Matches, int Conflicts)
{
    public bool HasComparableData => Matches + Conflicts > 0;

    /// <summary>null when nothing could be compared (neutral).</summary>
    public double? Score => HasComparableData ? (double)Matches / (Matches + Conflicts) : null;
}

public enum MatchConfidence
{
    Possible = 0,
    Likely = 1
}

public sealed record MatchResult(
    Guid MasterItemId,
    string CanonicalName,
    string MatchedName,
    double? TextScore,
    double? SemanticScore,
    double? AttributeScore,
    double FinalScore,
    MatchConfidence Confidence,
    IReadOnlyList<string> Reasons);
