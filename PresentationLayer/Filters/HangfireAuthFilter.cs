using Domain.Common;
using Hangfire.Dashboard;

namespace Presentation.Filters;

public class HangfireAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        return httpContext.User.IsInRole(nameof(UserRole.Admin));
    }
}
