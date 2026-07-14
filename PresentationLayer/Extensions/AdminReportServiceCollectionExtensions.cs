using Business.Services.Reports;
using Domain.Contracts;

namespace Presentation.Extensions;

public static class AdminReportServiceCollectionExtensions
{
    public static IServiceCollection AddAdminReports(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IAdminReportService, AdminReportService>();
        return services;
    }
}
