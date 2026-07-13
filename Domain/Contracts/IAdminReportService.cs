using Domain.Contracts.DTOs;

namespace Domain.Contracts;

public interface IAdminReportService
{
    Task<AdminReportDashboardDto> GetDashboardAsync(ReportRange range, ReportRoleFilter role, int? trendSubjectId, CancellationToken cancellationToken = default);
}
