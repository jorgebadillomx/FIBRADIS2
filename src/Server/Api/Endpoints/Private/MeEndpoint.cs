using System.Security.Claims;

namespace Api.Endpoints.Private;

public static class MeEndpoint
{
    public static IEndpointRouteBuilder MapMe(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/me", (ClaimsPrincipal user) =>
            Results.Ok(new { role = user.FindFirstValue(ClaimTypes.Role) }))
           .RequireAuthorization("User")
           .WithTags("Auth")
           .Produces<object>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }
}
