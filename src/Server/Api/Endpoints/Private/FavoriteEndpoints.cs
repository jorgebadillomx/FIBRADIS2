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
            var userId = GetUserId(ctx);
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
            var userId = GetUserId(ctx);
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
            var userId = GetUserId(ctx);
            await repo.RemoveAsync(userId, fibraId, ct);
            return Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static Guid GetUserId(HttpContext ctx) =>
        Guid.Parse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
