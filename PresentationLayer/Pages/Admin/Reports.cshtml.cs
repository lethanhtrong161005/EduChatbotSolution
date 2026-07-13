using Domain.Contracts;
using Domain.Contracts.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Presentation.Pages.Admin;

/// <summary>
/// Displays the admin adoption-and-health report dashboard.
/// Provides a AJAX handler that returns a fully pre-computed
/// <see cref="AdminReportDashboardDto"/> for the selected range, role, and optional trend subject.
/// </summary>
[Authorize(Roles = "Admin")]
public class ReportsModel(IAdminReportService reportService) : PageModel
{
    private readonly IAdminReportService _reportService = reportService;

    /// <summary>Renders the Reports page shell.</summary>
    public Task OnGetAsync(CancellationToken cxlTkn) => Task.CompletedTask;

    /// <summary>
    /// Returns the full dashboard DTO for the selected range, role filter, and optional trend subject.
    /// </summary>
    /// <param name="range">Time window: Last7Days (0), Last30Days (1), or AllTime (2).</param>
    /// <param name="role">User role filter: All (0), Student (1), Lecturer (2), or Admin (3).</param>
    /// <param name="trendSubjectId">
    /// Optional subject ID that filters only the three daily trend series.
    /// Null means all subjects. The cross-subject chart and table are never filtered.
    /// </param>
    /// <param name="cxlTkn">Cancellation token.</param>
    public async Task<IActionResult> OnGetDashboardAsync(
        [FromQuery] ReportRange range,
        [FromQuery] ReportRoleFilter role,
        [FromQuery] int? trendSubjectId,
        CancellationToken cxlTkn)
    {
        if (!Enum.IsDefined(range) || !Enum.IsDefined(role))
            return BadRequest(new { error = "Invalid range or role value." });

        var dashboard = await _reportService.GetDashboardAsync(range, role, trendSubjectId, cxlTkn);
        return new JsonResult(dashboard);
    }
}
