using Domain.Contracts;
using Domain.Contracts.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Presentation.Pages.Admin.Experiments;

/// <summary>
/// Displays a pairwise comparison between two completed experiment runs.
/// Shows aggregate RAGAS-style metric comparisons with deltas, and per-question
/// side-by-side scores.
/// </summary>
[Authorize(Roles = "Admin")]
public class CompareModel(IExperimentService experimentService) : PageModel
{
    private readonly IExperimentService _experimentService = experimentService;

    /// <summary>Left experiment ID (from query string), if provided.</summary>
    public Guid? InitialLeftId { get; private set; }

    /// <summary>Right experiment ID (from query string), if provided.</summary>
    public Guid? InitialRightId { get; private set; }

    /// <summary>
    /// Renders the comparison page shell.
    /// </summary>
    /// <param name="leftId">Optional left-side experiment GUID.</param>
    /// <param name="rightId">Optional right-side experiment GUID.</param>
    public Task OnGetAsync([FromQuery] Guid? leftId, [FromQuery] Guid? rightId, CancellationToken cxlTkn)
    {
        InitialLeftId = leftId;
        InitialRightId = rightId;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns a pairwise comparison DTO for two experiment runs.
    /// </summary>
    /// <param name="leftId">Left experiment GUID.</param>
    /// <param name="rightId">Right experiment GUID.</param>
    /// <returns>
    /// 200 with <see cref="ExperimentComparisonDto"/>,
    /// 400 when IDs are equal or parameters are invalid,
    /// 404 when either experiment is not found,
    /// 409 when experiments are incompatible (different subjects or question sets).
    /// </returns>
    public async Task<IActionResult> OnGetComparisonAsync(
        [FromQuery] Guid leftId,
        [FromQuery] Guid rightId,
        CancellationToken cxlTkn)
    {
        if (leftId == Guid.Empty || rightId == Guid.Empty)
            return BadRequest(new { error = "Both leftId and rightId must be non-empty GUIDs." });

        if (leftId == rightId)
            return BadRequest(new { error = "Cannot compare an experiment with itself." });

        var comparison = await _experimentService.CompareAsync(leftId, rightId, cxlTkn);
        if (comparison is null)
            return NotFound(new { error = "One or both experiments were not found." });

        return new JsonResult(comparison);
    }
}
