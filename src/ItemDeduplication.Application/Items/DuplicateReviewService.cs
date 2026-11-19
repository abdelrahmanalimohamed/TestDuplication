using ItemDeduplication.Application.Common.Interfaces;
using ItemDeduplication.Application.Common.Models;
using ItemDeduplication.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ItemDeduplication.Application.Items;

public sealed class DuplicateReviewService : IDuplicateReviewService
{
    private readonly IApplicationDbContext _db;
    private readonly IItemNormalizationService _normalizer;
    private readonly IAliasIndexer _indexer;

    public DuplicateReviewService(IApplicationDbContext db, IItemNormalizationService normalizer, IAliasIndexer indexer)
    {
        _db = db;
        _normalizer = normalizer;
        _indexer = indexer;
    }

    public async Task<IReadOnlyList<DuplicateCandidateDto>> GetPendingAsync(int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);

        return await _db.DuplicateCandidates.AsNoTracking()
            .Where(c => c.Status == DuplicateStatus.Pending)
            .OrderByDescending(c => c.FinalScore)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new DuplicateCandidateDto(
                c.Id, c.SourceSystem, c.SourceCode, c.SourceName,
                c.MasterItemId, c.MasterItem.CanonicalName,
                c.TextScore, c.SemanticScore, c.AttributeScore, c.FinalScore, c.CreatedAtUtc))
            .ToListAsync(ct);
    }

    /// <summary>"Yes, same item": attach the incoming code/name as an alias of the existing master item.</summary>
    public async Task ApproveAsync(Guid candidateId, string reviewer, string? notes, CancellationToken ct = default)
    {
        var candidate = await LoadPendingAsync(candidateId, ct);

        var alias = ItemFactory.CreateAlias(
            candidate.MasterItemId, candidate.SourceName, candidate.SourceSystem, candidate.SourceCode,
            _normalizer.Normalize(candidate.SourceName), isPrimary: false);

        _db.Aliases.Add(alias);
        MarkReviewed(candidate, DuplicateStatus.Approved, reviewer, notes);
        _db.AuditLogs.Add(ItemFactory.Audit("DuplicateCandidate", candidate.Id, "Approved",
            $"Merged into master {candidate.MasterItemId}. {notes}", reviewer));

        await _db.SaveChangesAsync(ct);
        await _indexer.TryIndexAsync(new[] { alias }, ct);
    }

    /// <summary>"No, different item": create a separate master item.</summary>
    public async Task RejectAsync(Guid candidateId, string reviewer, string? notes, CancellationToken ct = default)
    {
        var candidate = await LoadPendingAsync(candidateId, ct);

        var (master, alias) = ItemFactory.CreateMaster(
            candidate.SourceName, candidate.SourceSystem, candidate.SourceCode,
            _normalizer.Normalize(candidate.SourceName));

        _db.MasterItems.Add(master);
        MarkReviewed(candidate, DuplicateStatus.Rejected, reviewer, notes);
        _db.AuditLogs.Add(ItemFactory.Audit("DuplicateCandidate", candidate.Id, "Rejected",
            $"Created new master {master.Id}. {notes}", reviewer));

        await _db.SaveChangesAsync(ct);
        await _indexer.TryIndexAsync(new[] { alias }, ct);
    }

    private async Task<Domain.Entities.DuplicateCandidate> LoadPendingAsync(Guid id, CancellationToken ct)
    {
        var candidate = await _db.DuplicateCandidates.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new KeyNotFoundException($"Duplicate candidate {id} was not found.");

        if (candidate.Status != DuplicateStatus.Pending)
            throw new InvalidOperationException($"Candidate was already {candidate.Status}.");

        return candidate;
    }

    private static void MarkReviewed(Domain.Entities.DuplicateCandidate c, DuplicateStatus status, string reviewer, string? notes)
    {
        c.Status = status;
        c.ReviewedBy = reviewer;
        c.ReviewNotes = notes;
        c.ReviewedAtUtc = DateTime.UtcNow;
    }
}
