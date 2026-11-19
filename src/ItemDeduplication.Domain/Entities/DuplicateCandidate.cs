using ItemDeduplication.Domain.Enums;

namespace ItemDeduplication.Domain.Entities;

/// <summary>An incoming item that looks like an existing master item and waits for a human decision.</summary>
public class DuplicateCandidate
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string SourceSystem { get; set; } = default!;
    public string SourceCode { get; set; } = default!;
    public string SourceName { get; set; } = default!;
    public string NormalizedName { get; set; } = default!;

    public Guid MasterItemId { get; set; }
    public MasterItem MasterItem { get; set; } = default!;

    public double? TextScore { get; set; }
    public double? SemanticScore { get; set; }
    public double? AttributeScore { get; set; }
    public double FinalScore { get; set; }

    public DuplicateStatus Status { get; set; } = DuplicateStatus.Pending;
    public string? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
