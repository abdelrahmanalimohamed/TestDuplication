using ItemDeduplication.Application.Common.Interfaces;
using ItemDeduplication.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ItemDeduplication.Application.Items;

public sealed class AliasIndexer : IAliasIndexer
{
    private const int BatchSize = 64;

    private readonly IApplicationDbContext _db;
    private readonly IEmbeddingService _embeddings;
    private readonly IVectorStore _vectors;
    private readonly ILogger<AliasIndexer> _logger;

    public AliasIndexer(IApplicationDbContext db, IEmbeddingService embeddings, IVectorStore vectors, ILogger<AliasIndexer> logger)
    {
        _db = db;
        _embeddings = embeddings;
        _vectors = vectors;
        _logger = logger;
    }

    public async Task<bool> TryIndexAsync(IReadOnlyCollection<ItemAlias> aliases, CancellationToken ct = default)
    {
        try
        {
            await IndexAsync(aliases, ct);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // DB is the source of truth. A failed index can be repaired with ReindexAllAsync.
            _logger.LogError(ex, "Failed to index {Count} alias(es) in the vector store.", aliases.Count);
            return false;
        }
    }

    public async Task<int> ReindexAllAsync(CancellationToken ct = default)
    {
        await _vectors.EnsureCollectionAsync(ct);

        var total = 0;
        while (true)
        {
            var batch = await _db.Aliases.AsNoTracking()
                .OrderBy(a => a.Id)
                .Skip(total)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (batch.Count == 0) break;

            await IndexAsync(batch, ct);
            total += batch.Count;
        }

        return total;
    }

    private async Task IndexAsync(IReadOnlyCollection<ItemAlias> aliases, CancellationToken ct)
    {
        if (aliases.Count == 0) return;

        var list = aliases.ToList();
        var vectors = await _embeddings.EmbedBatchAsync(list.Select(a => a.NormalizedName).ToList(), ct);

        var points = list
            .Zip(vectors, (alias, vector) => new VectorPoint(alias.Id, vector, alias.MasterItemId))
            .ToList();

        await _vectors.UpsertAsync(points, ct);
    }
}
