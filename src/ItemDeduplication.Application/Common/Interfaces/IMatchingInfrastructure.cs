using ItemDeduplication.Application.Common.Models;

namespace ItemDeduplication.Application.Common.Interfaces;

/// <summary>Fuzzy string similarity (FuzzySharp). Returns 0..1.</summary>
public interface ITextMatcher
{
    double Score(string a, string b);
}

/// <summary>Turns text into a multilingual embedding vector.</summary>
public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);
    Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct = default);
}

public sealed record VectorPoint(Guid AliasId, float[] Vector, Guid MasterItemId);
public sealed record VectorHit(Guid AliasId, Guid MasterItemId, double Score);

/// <summary>Vector database abstraction (Qdrant today, pgvector tomorrow).</summary>
public interface IVectorStore
{
    Task EnsureCollectionAsync(CancellationToken ct = default);
    Task UpsertAsync(IReadOnlyCollection<VectorPoint> points, CancellationToken ct = default);
    Task<IReadOnlyList<VectorHit>> SearchAsync(float[] vector, int limit, double minScore, CancellationToken ct = default);
}

public interface IAttributeExtractor
{
    /// <summary>Extracts measurable attributes (size, weight, volume, voltage...) from a NORMALIZED name.</summary>
    IReadOnlyList<ItemAttribute> Extract(string normalizedName);
    AttributeComparison Compare(IReadOnlyList<ItemAttribute> a, IReadOnlyList<ItemAttribute> b);
}
