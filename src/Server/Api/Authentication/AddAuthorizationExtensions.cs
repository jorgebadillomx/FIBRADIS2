using System.Security.Claims;

namespace Api.Authentication;

public static class AddAuthorizationExtensions
{
    public static WebApplicationBuilder AddFibradisAuthorization(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("User", policy =>
                policy.RequireAuthenticatedUser());

            options.AddPolicy("AdminOps", policy =>
                policy.RequireClaim(ClaimTypes.Role, "AdminOps"));
        });

        return builder;
    }
}
