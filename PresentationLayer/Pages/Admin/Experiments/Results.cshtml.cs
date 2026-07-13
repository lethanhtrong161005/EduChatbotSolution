using Domain.Contracts;
using Domain.Contracts.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Presentation.Pages.Admin.Experiments;

/// <summary>
/// Displays the experiment results/history page.
/// Lists every experiment run and provides a detail view for a selected run,
/// including progress for in-flight experiments and question-level results for completed runs.
/// </summary>
[Authorize(Roles = "Admin")]
public class ResultsModel(IExperimentService experimentService) : PageModel
{
    private readonly IExperimentService _experimentService = experimentService;

    /// <summary>
    /// Optional experiment ID supplied via query string for deep-linking
    /// directly to a specific experiment's detail panel.
    /// </summary>
    public Guid? InitialExperimentId { get; private set; }

    /// <summary>Renders the Results page shell.</summary>
    /// <param name="id">Optional experiment GUID to pre-select on load.</param>
    public Task OnGetAsync([FromQuery] Guid? id, CancellationToken cxlTkn)
    {
        InitialExperimentId = id;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns all experiment summaries for the history table.
    /// </summary>
    public async Task<IActionResult> OnGetSummariesAsync(CancellationToken cxlTkn)
    {
        var summaries = await _experimentService.GetSummariesAsync(cxlTkn);
        return new JsonResult(summaries);
    }

    /// <summary>
    /// Returns the full result DTO for a single experiment.
    /// Used both for polling in-progress runs and for viewing completed results.
    /// </summary>
    /// <param name="id">The experiment GUID.</param>
    public async Task<IActionResult> OnGetResultAsync([FromQuery] Guid id, CancellationToken cxlTkn)
    {
        var result = await _experimentService.GetResultAsync(id, cxlTkn);
        if (result is null)
            return NotFound(new { error = $"Experiment {id} not found." });

        return new JsonResult(result);
    }
}
