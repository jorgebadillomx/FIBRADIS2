using Hangfire.Dashboard;
using System.Security.Claims;

namespace Api.Hangfire;

public class HangfireDashboardAuthFilter(IWebHostEnvironment env) : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        if (env.IsDevelopment())
            return true;

        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
               && httpContext.User.HasClaim(ClaimTypes.Role, "AdminOps");
    }
}
