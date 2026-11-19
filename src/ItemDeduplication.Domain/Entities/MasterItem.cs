namespace ItemDeduplication.Domain.Entities;

/// <summary>The canonical "real world" item. Every source code/name points to one of these.</summary>
public class MasterItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CanonicalName { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<ItemAlias> Aliases { get; set; } = new();
}
