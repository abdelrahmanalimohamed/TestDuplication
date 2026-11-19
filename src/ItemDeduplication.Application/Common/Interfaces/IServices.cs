using ItemDeduplication.Application.Common.Models;
using ItemDeduplication.Domain.Entities;

namespace ItemDeduplication.Application.Common.Interfaces;

public interface IMatchingEngine
{
    Task<IReadOnlyList<MatchResult>> FindMatchesAsync(string itemName, CancellationToken ct = default);
}

public interface IAliasIndexer
{
    /// <summary>Embeds + upserts aliases. Never throws for infrastructure failures; returns false instead.</summary>
    Task<bool> TryIndexAsync(IReadOnlyCollection<ItemAlias> aliases, CancellationToken ct = default);
    Task<int> ReindexAllAsync(CancellationToken ct = default);
}

public interface IItemIntakeService
{
    Task<IReadOnlyList<MatchResult>> CheckAsync(string itemName, CancellationToken ct = default);
    Task<ImportOutcome> ImportAsync(ItemInput input, CancellationToken ct = default);
}

public interface IDuplicateReviewService
{
    Task<IReadOnlyList<DuplicateCandidateDto>> GetPendingAsync(int page, int pageSize, CancellationToken ct = default);
    Task ApproveAsync(Guid candidateId, string reviewer, string? notes, CancellationToken ct = default);
    Task RejectAsync(Guid candidateId, string reviewer, string? notes, CancellationToken ct = default);
}
