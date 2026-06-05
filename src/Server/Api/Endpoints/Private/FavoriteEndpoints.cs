using System.Security.Claims;
using Application.Portfolio;
using SharedApiContracts.Portfolio;

namespace Api.Endpoints.Private;

public static class FavoriteEndpoints
{
    public static IEndpointRouteBuilder MapFavorites(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/portfolio/favorites")
            .RequireAuthorization("User")
            .WithTags("Favorites");

        group.MapGet("/", async (
            IUserFavoritesRepository repo,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (TryGetUserId(ctx) is not { } userId)
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);

            var ids = await repo.GetFavoriteIdsAsync(userId, ct);
            return Results.Ok(new UserFavoritesDto(ids));
        })
        .Produces<UserFavoritesDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPut("/{fibraId:guid}", async (
            Guid fibraId,
            IUserFavoritesRepository repo,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (TryGetUserId(ctx) is not { } userId)
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);

            await repo.AddAsync(userId, fibraId, ct);
            return Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapDelete("/{fibraId:guid}", async (
            Guid fibraId,
            IUserFavoritesRepository repo,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (TryGetUserId(ctx) is not { } userId)
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);

            await repo.RemoveAsync(userId, fibraId, ct);
            return Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static Guid? TryGetUserId(HttpContext ctx) =>
        Guid.TryParse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
