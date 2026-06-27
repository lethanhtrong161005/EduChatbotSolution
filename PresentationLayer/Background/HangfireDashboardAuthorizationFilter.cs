using Domain.Common;
using Hangfire.Dashboard;

namespace Presentation.Background;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        return httpContext.User.IsInRole(nameof(UserRole.Admin));
    }
}
