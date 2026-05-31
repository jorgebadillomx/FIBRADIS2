using Application.Ops;
using SharedApiContracts.Editorial;

namespace Api.Endpoints.Ops;

public static class OpsEditorialEndpoints
{
    public static IEndpointRouteBuilder MapOpsEditorial(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ops")
            .RequireAuthorization("AdminOps")
            .WithTags("Ops");

        group.MapPut("/pages/{slug}", async (
            string slug,
            UpdateEditorialPageRequest request,
            IEditorialPageRepository repo,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["content"] = ["content es requerido."]
                });
            }

            if (request.Content.Length > 100_000)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["content"] = ["El contenido no puede superar 100 000 caracteres."]
                });
            }

            var rows = await repo.UpdateContentAsync(slug, request.Content, ct);
            return rows == 0 ? Results.NotFound() : Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
