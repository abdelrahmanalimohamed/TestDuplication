using ItemDeduplication.Application.Common.Interfaces;
using ItemDeduplication.Application.Common.Models;
using ItemDeduplication.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ItemDeduplication.Application.Matching;

public sealed class MatchingEngine : IMatchingEngine
{
    private readonly IApplicationDbContext _db;
    private readonly IItemNormalizationService _normalizer;
    private readonly IEmbeddingService _embeddings;
    private readonly IVectorStore _vectors;
    private readonly ITextMatcher _text;
    private readonly IAttributeExtractor _attributes;
    private readonly ScoreCalculator _scorer;
    private readonly MatchingOptions _options;
    private readonly ILogger<MatchingEngine> _logger;

    public MatchingEngine(
        IApplicationDbContext db,
        IItemNormalizationService normalizer,
        IEmbeddingService embeddings,
        IVectorStore vectors,
        ITextMatcher text,
        IAttributeExtractor attributes,
        ScoreCalculator scorer,
        IOptions<MatchingOptions> options,
        ILogger<MatchingEngine> logger)
    {
        _db = db;
        _normalizer = normalizer;
        _embeddings = embeddings;
        _vectors = vectors;
        _text = text;
        _attributes = attributes;
        _scorer = scorer;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MatchResult>> FindMatchesAsync(string itemName, CancellationToken ct = default)
    {
        var query = _normalizer.Normalize(itemName);
        if (query.NormalizedName.Length == 0) return Array.Empty<MatchResult>();

        // 1) Semantic candidates (cross-language: "vacuum" <-> "مكنسة")
        var vectorHits = await SearchVectorsAsync(query.NormalizedName, ct);
        var semanticByAlias = vectorHits.GroupBy(h => h.AliasId).ToDictionary(g => g.Key, g => g.Max(h => h.Score));

        // 2) Lexical candidates (typos, word order, same-language variants)
        var candidates = await GetTextCandidatesAsync(query, ct);

        var missing = semanticByAlias.Keys.Where(id => !candidates.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            var extra = await _db.Aliases.AsNoTracking()
                .Include(a => a.MasterItem)
                .Where(a => missing.Contains(a.Id))
                .ToListAsync(ct);

            foreach (var alias in extra) candidates[alias.Id] = alias;
        }

        // 3) Score every candidate with all three signals
        var queryAttributes = _attributes.Extract(query.NormalizedName);
        var scored = new List<MatchResult>();

        foreach (var alias in candidates.Values)
        {
            var crossLanguage = ScriptHelper.IsCrossLanguage(query.NormalizedName, alias.NormalizedName);

            double? text = crossLanguage ? null : _text.Score(query.NormalizedName, alias.NormalizedName);
            double? semantic = semanticByAlias.TryGetValue(alias.Id, out var raw) ? Rescale(raw) : null;
            var attributes = _attributes.Compare(queryAttributes, _attributes.Extract(alias.NormalizedName));

            var final = _scorer.Combine(text, semantic, attributes);
            if (final < _options.ReviewThreshold) continue;

            scored.Add(new MatchResult(
                alias.MasterItemId,
                alias.MasterItem.CanonicalName,
                alias.Name,
                text,
                semantic,
                attributes.Score,
                Math.Round(final, 4),
                final >= _options.HighConfidenceThreshold ? MatchConfidence.Likely : MatchConfidence.Possible,
                BuildReasons(text, semantic, attributes, crossLanguage)));
        }

        // 4) One result per master item (its best-scoring alias)
        return scored
            .GroupBy(r => r.MasterItemId)
            .Select(g => g.OrderByDescending(r => r.FinalScore).First())
            .OrderByDescending(r => r.FinalScore)
            .Take(_options.MaxResults)
            .ToList();
    }

    private async Task<IReadOnlyList<VectorHit>> SearchVectorsAsync(string normalizedName, CancellationToken ct)
    {
        try
        {
            var vector = await _embeddings.EmbedAsync(normalizedName, ct);
            return await _vectors.SearchAsync(vector, _options.VectorTopK, _options.VectorMinScore, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Graceful degradation: fall back to text + attributes only.
            _logger.LogWarning(ex, "Semantic search unavailable, continuing with text matching only.");
            return Array.Empty<VectorHit>();
        }
    }

    private async Task<Dictionary<Guid, ItemAlias>> GetTextCandidatesAsync(NormalizedItem query, CancellationToken ct)
    {
        var found = new Dictionary<Guid, ItemAlias>();

        var tokens = query.Tokens
            .Where(t => t.Length >= 2 && !t.All(char.IsDigit))
            .OrderByDescending(t => t.Length)
            .Take(4)
            .ToList();

        // NOTE: LIKE '%token%' is fine up to ~100k aliases. Beyond that use SQL Server Full-Text Search.
        foreach (var token in tokens)
        {
            var rows = await _db.Aliases.AsNoTracking()
                .Include(a => a.MasterItem)
                .Where(a => a.NormalizedName.Contains(token))
                .Take(_options.MaxTextCandidates)
                .ToListAsync(ct);

            foreach (var row in rows) found.TryAdd(row.Id, row);
        }

        return found;
    }

    private double Rescale(double rawCosine)
    {
        var range = _options.SemanticCeiling - _options.SemanticFloor;
        return Math.Clamp((rawCosine - _options.SemanticFloor) / range, 0, 1);
    }

    private static IReadOnlyList<string> BuildReasons(
        double? text, double? semantic, AttributeComparison attributes, bool crossLanguage)
    {
        var reasons = new List<string>();

        if (crossLanguage) reasons.Add("Different languages: matched by meaning, not spelling");
        if (text is not null) reasons.Add($"Text similarity {text.Value:P0}");
        if (semantic is not null) reasons.Add($"Semantic similarity {semantic.Value:P0}");
        if (attributes.Conflicts > 0) reasons.Add("Attribute conflict (size/weight/volume differs)");
        else if (attributes.Matches > 0) reasons.Add("Attributes match (size/weight/volume)");

        return reasons;
    }
}
