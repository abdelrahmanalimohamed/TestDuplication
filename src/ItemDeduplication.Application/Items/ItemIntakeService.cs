using ItemDeduplication.Application.Common.Interfaces;
using ItemDeduplication.Application.Common.Models;
using ItemDeduplication.Application.Matching;
using ItemDeduplication.Domain.Entities;
using ItemDeduplication.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ItemDeduplication.Application.Items;

public sealed class ItemIntakeService : IItemIntakeService
{
    private readonly IApplicationDbContext _db;
    private readonly IMatchingEngine _engine;
    private readonly IItemNormalizationService _normalizer;
    private readonly IAliasIndexer _indexer;
    private readonly MatchingOptions _options;

    public ItemIntakeService(
        IApplicationDbContext db,
        IMatchingEngine engine,
        IItemNormalizationService normalizer,
        IAliasIndexer indexer,
        IOptions<MatchingOptions> options)
    {
        _db = db;
        _engine = engine;
        _normalizer = normalizer;
        _indexer = indexer;
        _options = options.Value;
    }

    /// <summary>Read-only: "are there similar items?" - call this while the user is typing in the create form.</summary>
    public Task<IReadOnlyList<MatchResult>> CheckAsync(string itemName, CancellationToken ct = default)
        => _engine.FindMatchesAsync(itemName, ct);

    public async Task<ImportOutcome> ImportAsync(ItemInput input, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
            throw new ArgumentException("Item name is required.", nameof(input));
        if (string.IsNullOrWhiteSpace(input.SourceSystem) || string.IsNullOrWhiteSpace(input.SourceCode))
            throw new ArgumentException("SourceSystem and SourceCode are required.", nameof(input));

        // Idempotency: same system + code imported before
        var existing = await _db.Aliases.AsNoTracking().FirstOrDefaultAsync(
            a => a.SourceSystem == input.SourceSystem && a.SourceCode == input.SourceCode, ct);
        if (existing is not null)
            return new ImportOutcome(ImportStatus.AlreadyExists, existing.MasterItemId, null, Array.Empty<MatchResult>());

        var pending = await _db.DuplicateCandidates.AsNoTracking().FirstOrDefaultAsync(
            c => c.Status == DuplicateStatus.Pending &&
                 c.SourceSystem == input.SourceSystem && c.SourceCode == input.SourceCode, ct);
        if (pending is not null)
            return new ImportOutcome(ImportStatus.PendingReview, pending.MasterItemId, pending.Id, Array.Empty<MatchResult>());

        var normalized = _normalizer.Normalize(input.Name);
        var matches = await _engine.FindMatchesAsync(input.Name, ct);
        var best = matches.FirstOrDefault();

        // Near-certain duplicate -> attach automatically
        if (best is not null && best.FinalScore >= _options.AutoLinkThreshold)
        {
            var alias = ItemFactory.CreateAlias(best.MasterItemId, input.Name, input.SourceSystem, input.SourceCode, normalized, false);
            _db.Aliases.Add(alias);
            _db.AuditLogs.Add(ItemFactory.Audit("ItemAlias", alias.Id, "AutoLinked",
                $"'{input.Name}' linked to master {best.MasterItemId} (score {best.FinalScore:P1})"));
            await _db.SaveChangesAsync(ct);
            await _indexer.TryIndexAsync(new[] { alias }, ct);

            return new ImportOutcome(ImportStatus.AutoLinked, best.MasterItemId, null, matches);
        }

        // Probable duplicate -> human review
        if (best is not null)
        {
            var candidate = new DuplicateCandidate
            {
                SourceSystem = input.SourceSystem.Trim(),
                SourceCode = input.SourceCode.Trim(),
                SourceName = input.Name.Trim(),
                NormalizedName = normalized.NormalizedName,
                MasterItemId = best.MasterItemId,
                TextScore = best.TextScore,
                SemanticScore = best.SemanticScore,
                AttributeScore = best.AttributeScore,
                FinalScore = best.FinalScore
            };

            _db.DuplicateCandidates.Add(candidate);
            _db.AuditLogs.Add(ItemFactory.Audit("DuplicateCandidate", candidate.Id, "Flagged",
                $"'{input.Name}' looks like '{best.CanonicalName}' (score {best.FinalScore:P1})"));
            await _db.SaveChangesAsync(ct);

            return new ImportOutcome(ImportStatus.PendingReview, best.MasterItemId, candidate.Id, matches);
        }

        // Nothing similar -> new master item
        var (master, newAlias) = ItemFactory.CreateMaster(input.Name, input.SourceSystem, input.SourceCode, normalized);
        _db.MasterItems.Add(master);
        _db.AuditLogs.Add(ItemFactory.Audit("MasterItem", master.Id, "Created", $"From {input.SourceSystem}/{input.SourceCode}"));
        await _db.SaveChangesAsync(ct);
        await _indexer.TryIndexAsync(new[] { newAlias }, ct);

        return new ImportOutcome(ImportStatus.Created, master.Id, null, Array.Empty<MatchResult>());
    }
}
