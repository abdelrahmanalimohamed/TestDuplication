using ItemDeduplication.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ItemDeduplication.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<MasterItem> MasterItems { get; }
    DbSet<ItemAlias> Aliases { get; }
    DbSet<DuplicateCandidate> DuplicateCandidates { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
