using Application.News;
using Microsoft.EntityFrameworkCore;
using SharedApiContracts.News;

namespace Api.Endpoints.Ops;

public static class NewsBlocklistEndpoints
{
    private const int MaxBlocklistTermLength = 256;

    public static IEndpointRouteBuilder MapNewsBlocklist(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/news/blocklist-terms")
            .RequireAuthorization("AdminOps")
            .WithTags("News");

        group.MapGet("/", async (
            IBlocklistRepository blocklistRepository,
            CancellationToken ct) =>
        {
            var terms = await blocklistRepository.GetAllAsync(ct);
            var response = terms
                .Select(term => new BlocklistTermDto(term.Id, term.Term, term.CreatedAt))
                .ToList();

            return Results.Ok(response);
        })
        .Produces<IReadOnlyList<BlocklistTermDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/", async (
            CreateBlocklistTermRequest request,
            IBlocklistRepository blocklistRepository,
            CancellationToken ct) =>
        {
            var term = request.Term?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(term))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["term"] = ["El término es obligatorio."],
                });
            }

            if (term.Length > MaxBlocklistTermLength)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["term"] = [$"El término no puede exceder {MaxBlocklistTermLength} caracteres."],
                });
            }

            if (await blocklistRepository.ExistsAsync(term, ct))
            {
                return Results.Conflict(new
                {
                    message = "El término ya existe en el blocklist.",
                });
            }

            try
            {
                var created = await blocklistRepository.AddAsync(term, ct);
                return Results.Created(
                    $"/api/v1/news/blocklist-terms/{created.Id}",
                    new BlocklistTermDto(created.Id, created.Term, created.CreatedAt));
            }
            catch (DbUpdateException)
            {
                return Results.Conflict(new
                {
                    message = "El término ya existe en el blocklist.",
                });
            }
        })
        .Produces<BlocklistTermDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapDelete("/{id:guid}", async (
            Guid id,
            IBlocklistRepository blocklistRepository,
            CancellationToken ct) =>
        {
            var deleted = await blocklistRepository.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }
}
