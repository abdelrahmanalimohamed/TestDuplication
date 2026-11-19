namespace ItemDeduplication.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntityType { get; set; } = default!;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = default!;
    public string? Details { get; set; }
    public string PerformedBy { get; set; } = "system";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
