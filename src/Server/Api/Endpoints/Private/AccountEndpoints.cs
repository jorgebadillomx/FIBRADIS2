using Application.Auth;
using Domain.Auth.Exceptions;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace Api.Endpoints.Private;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccount(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/account/accept-terms", async (
            IUserService svc,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!Guid.TryParse(sub, out var userId))
                return Results.Unauthorized();

            try
            {
                await svc.AcceptTermsAsync(userId, ct);
            }
            catch (UserNotFoundException)
            {
                return Results.Unauthorized();
            }

            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithTags("Account")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }
}
