using Application.Ops;
using SharedApiContracts.Editorial;

namespace Api.Endpoints.Public;

public static class EditorialEndpoints
{
    public static IEndpointRouteBuilder MapEditorial(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/pages", async (
            IEditorialPageRepository repo,
            CancellationToken ct) =>
        {
            var pages = await repo.GetAllAsync(ct);
            var dtos = pages.Select(page => new EditorialPageDto(
                page.Slug,
                page.Title,
                page.Content,
                page.UpdatedAt));

            return Results.Ok(dtos);
        })
        .WithTags("Editorial")
        .AllowAnonymous()
        .Produces<IReadOnlyList<EditorialPageDto>>(StatusCodes.Status200OK);

        return app;
    }
}
