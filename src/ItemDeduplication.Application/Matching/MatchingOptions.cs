namespace ItemDeduplication.Application.Matching;

public sealed class MatchingOptions
{
    public const string SectionName = "Matching";

    // Score weights (renormalised automatically when a signal is unavailable)
    public double TextWeight { get; set; } = 0.30;
    public double SemanticWeight { get; set; } = 0.50;
    public double AttributeWeight { get; set; } = 0.20;

    /// <summary>Final score is multiplied by this when attributes conflict (60 cm vs 80 cm).</summary>
    public double ConflictPenalty { get; set; } = 0.60;

    public double ReviewThreshold { get; set; } = 0.65;
    public double HighConfidenceThreshold { get; set; } = 0.85;
    /// <summary>Above this the item is attached automatically. Set above 1.0 to disable auto-linking.</summary>
    public double AutoLinkThreshold { get; set; } = 0.98;

    public int VectorTopK { get; set; } = 20;
    public double VectorMinScore { get; set; } = 0.35;

    // Raw cosine similarity is rescaled: floor -> 0, ceiling -> 1. Tune with your own data.
    public double SemanticFloor { get; set; } = 0.35;
    public double SemanticCeiling { get; set; } = 0.90;

    public int MaxTextCandidates { get; set; } = 100;
    public int MaxResults { get; set; } = 5;
}
