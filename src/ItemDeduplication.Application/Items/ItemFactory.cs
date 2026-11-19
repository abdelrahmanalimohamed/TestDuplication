using ItemDeduplication.Application.Common.Models;
using ItemDeduplication.Domain.Entities;

namespace ItemDeduplication.Application.Items;

internal static class ItemFactory
{
    public static ItemAlias CreateAlias(Guid masterId, string name, string sourceSystem, string sourceCode,
        NormalizedItem normalized, bool isPrimary) => new()
    {
        MasterItemId = masterId,
        Name = name.Trim(),
        NormalizedName = normalized.NormalizedName,
        SourceSystem = sourceSystem.Trim(),
        SourceCode = sourceCode.Trim(),
        IsPrimary = isPrimary
    };

    public static (MasterItem Master, ItemAlias Alias) CreateMaster(string name, string sourceSystem,
        string sourceCode, NormalizedItem normalized)
    {
        var master = new MasterItem { CanonicalName = name.Trim() };
        var alias = CreateAlias(master.Id, name, sourceSystem, sourceCode, normalized, isPrimary: true);
        master.Aliases.Add(alias);
        return (master, alias);
    }

    public static AuditLog Audit(string entityType, Guid entityId, string action, string? details, string by = "system") => new()
    {
        EntityType = entityType,
        EntityId = entityId,
        Action = action,
        Details = details,
        PerformedBy = by
    };
}
