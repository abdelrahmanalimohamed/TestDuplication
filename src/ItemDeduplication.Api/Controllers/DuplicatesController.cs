using ItemDeduplication.Api.Contracts;
using ItemDeduplication.Application.Common.Interfaces;
using ItemDeduplication.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace ItemDeduplication.Api.Controllers;

[ApiController]
[Route("api/duplicates")]
public sealed class DuplicatesController : ControllerBase
{
    private readonly IDuplicateReviewService _review;

    public DuplicatesController(IDuplicateReviewService review) => _review = review;

    [HttpGet("pending")]
    public async Task<ActionResult<IReadOnlyList<DuplicateCandidateDto>>> Pending(
        int page = 1, int pageSize = 50, CancellationToken ct = default)
        => Ok(await _review.GetPendingAsync(page, pageSize, ct));

    /// <summary>Same item -> merge as alias of the existing master item.</summary>
    [HttpPost("{id:guid}/approve")]
    public Task<IActionResult> Approve(Guid id, ReviewRequest request, CancellationToken ct)
        => Run(() => _review.ApproveAsync(id, request.Reviewer ?? "unknown", request.Notes, ct));

    /// <summary>Different item -> create a separate master item.</summary>
    [HttpPost("{id:guid}/reject")]
    public Task<IActionResult> Reject(Guid id, ReviewRequest request, CancellationToken ct)
        => Run(() => _review.RejectAsync(id, request.Reviewer ?? "unknown", request.Notes, ct));

    private async Task<IActionResult> Run(Func<Task> action)
    {
        try
        {
            await action();
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }
}
