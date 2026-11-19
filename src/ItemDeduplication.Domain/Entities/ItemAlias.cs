namespace ItemDeduplication.Domain.Entities;

/// <summary>
/// One name/code a master item is known by (ERP code, SAP code, Arabic name, English name...).
/// The alias Id is also used as the vector point id in the vector database.
/// </summary>
public class ItemAlias
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MasterItemId { get; set; }
    public MasterItem MasterItem { get; set; } = default!;

    public string Name { get; set; } = default!;
    public string NormalizedName { get; set; } = default!;
    public string SourceSystem { get; set; } = default!;
    public string SourceCode { get; set; } = default!;
    public bool IsPrimary { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
