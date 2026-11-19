using ItemDeduplication.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ItemDeduplication.Api.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly IAliasIndexer _indexer;

    public AdminController(IAliasIndexer indexer) => _indexer = indexer;

    /// <summary>Rebuilds the vector index from SQL. Use after changing the embedding model or if Qdrant was down.</summary>
    [HttpPost("reindex")]
    public async Task<IActionResult> Reindex(CancellationToken ct)
        => Ok(new { indexed = await _indexer.ReindexAllAsync(ct) });
}
