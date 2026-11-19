using ItemDeduplication.Api.Contracts;
using ItemDeduplication.Application.Common.Interfaces;
using ItemDeduplication.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace ItemDeduplication.Api.Controllers;

[ApiController]
[Route("api/items")]
public sealed class ItemsController : ControllerBase
{
    private readonly IItemIntakeService _intake;

    public ItemsController(IItemIntakeService intake) => _intake = intake;

    /// <summary>Read-only duplicate check. Use it in the "create item" screen before saving.</summary>
    [HttpPost("check")]
    public async Task<ActionResult<IReadOnlyList<MatchResult>>> Check(CheckItemRequest request, CancellationToken ct)
        => Ok(await _intake.CheckAsync(request.Name, ct));

    /// <summary>Import one item: creates a master item, auto-links, or flags it for review.</summary>
    [HttpPost("import")]
    public async Task<ActionResult<ImportOutcome>> Import(ImportItemRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _intake.ImportAsync(new ItemInput(request.SourceSystem, request.SourceCode, request.Name), ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Bulk import (ERP/SAP/Excel extracts). Processed sequentially so items dedupe against each other.</summary>
    [HttpPost("import-bulk")]
    public async Task<ActionResult> ImportBulk(List<ImportItemRequest> requests, CancellationToken ct)
    {
        var summary = new Dictionary<string, int>();
        var errors = new List<object>();

        foreach (var r in requests)
        {
            try
            {
                var outcome = await _intake.ImportAsync(new ItemInput(r.SourceSystem, r.SourceCode, r.Name), ct);
                var key = outcome.Status.ToString();
                summary[key] = summary.GetValueOrDefault(key) + 1;
            }
            catch (ArgumentException ex)
            {
                errors.Add(new { r.SourceSystem, r.SourceCode, error = ex.Message });
            }
        }

        return Ok(new { summary, errors });
    }
}
