namespace ItemDeduplication.Application.Common.Models;

public sealed record NormalizedItem(
    string OriginalName,
    string NormalizedName,
    IReadOnlyList<string> Tokens
);
