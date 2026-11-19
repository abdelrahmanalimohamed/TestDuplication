namespace ItemDeduplication.Api.Contracts;

public sealed record CheckItemRequest(string Name);
public sealed record ImportItemRequest(string SourceSystem, string SourceCode, string Name);
public sealed record ReviewRequest(string? Reviewer, string? Notes);
