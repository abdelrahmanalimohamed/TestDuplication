namespace ItemDeduplication.Application.Common.Models;

public sealed record ItemInput(string SourceSystem, string SourceCode, string Name);

public enum ImportStatus
{
    Created,        // brand-new master item
    PendingReview,  // looks like an existing item -> waiting for approval
    AutoLinked,     // near-certain duplicate -> attached automatically
    AlreadyExists   // same source system + code was imported before
}

public sealed record ImportOutcome(
    ImportStatus Status,
    Guid? MasterItemId,
    Guid? DuplicateCandidateId,
    IReadOnlyList<MatchResult> Matches);

public sealed record DuplicateCandidateDto(
    Guid Id,
    string SourceSystem,
    string SourceCode,
    string SourceName,
    Guid MatchedMasterItemId,
    string MatchedCanonicalName,
    double? TextScore,
    double? SemanticScore,
    double? AttributeScore,
    double FinalScore,
    DateTime CreatedAtUtc);
