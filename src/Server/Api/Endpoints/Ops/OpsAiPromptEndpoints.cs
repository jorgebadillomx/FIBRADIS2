using System.Security.Claims;
using Application.News;
using Infrastructure.Integrations.Ai;
using SharedApiContracts.News;

namespace Api.Endpoints.Ops;

public static class OpsAiPromptEndpoints
{
    private static readonly string[] AllowedContentTypes =
    [
        AiPromptTemplateDefaults.NewsContentType,
        AiPromptTemplateDefaults.KpiExtractionContentType,
    ];

    private static readonly string[] NewsRequiredPlaceholders = ["{title}", "{snippet_section}", "{body_section}"];
    private static readonly string[] KpiRequiredPlaceholders = ["{markdown_content}"];

    public static IEndpointRouteBuilder MapOpsAiPrompts(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ops/ai-prompts")
            .RequireAuthorization("AdminOps")
            .WithTags("AI");

        group.MapGet("/{contentType}", async (
            string contentType,
            IAiPromptRepository repo,
            CancellationToken ct) =>
        {
            if (!IsAllowedContentType(contentType))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["contentType"] = ["Valor inválido. Use 'news' o 'kpi_extraction'."],
                });
            }

            var prompt = await repo.GetPromptAsync(contentType, ct);
            if (prompt is null)
            {
                return Results.Ok(new AiPromptDto(
                    contentType,
                    AiPromptTemplateDefaults.GetTemplate(contentType),
                    DateTimeOffset.UtcNow,
                    "system"));
            }

            return Results.Ok(new AiPromptDto(
                prompt.ContentType,
                prompt.PromptTemplate,
                prompt.UpdatedAt,
                prompt.UpdatedBy));
        })
        .Produces<AiPromptDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPut("/{contentType}", async (
            string contentType,
            UpdateAiPromptRequest request,
            IAiPromptRepository repo,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (!IsAllowedContentType(contentType))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["contentType"] = ["Valor inválido. Use 'news' o 'kpi_extraction'."],
                });
            }

            if (string.IsNullOrWhiteSpace(request.PromptTemplate))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["promptTemplate"] = ["El template no puede estar vacío."],
                });
            }

            var isKpi = string.Equals(contentType, AiPromptTemplateDefaults.KpiExtractionContentType, StringComparison.OrdinalIgnoreCase);
            var maxChars = isKpi ? 10_000 : 4_000;

            if (request.PromptTemplate.Length > maxChars)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["promptTemplate"] = [$"El template no puede superar los {maxChars} caracteres."],
                });
            }

            var requiredPlaceholders = isKpi ? KpiRequiredPlaceholders : NewsRequiredPlaceholders;
            var missing = requiredPlaceholders
                .Where(placeholder => !request.PromptTemplate.Contains(placeholder, StringComparison.Ordinal))
                .ToArray();
            if (missing.Length > 0)
            {
                var expected = isKpi
                    ? "{markdown_content}"
                    : "{title}, {snippet_section}, {body_section}";
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["promptTemplate"] = [$"El template debe contener los placeholders: {expected}"],
                });
            }

            var actor = ctx.User.Identity?.Name
                ?? ctx.User.FindFirstValue(ClaimTypes.Email)
                ?? ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "unknown";

            await repo.SetPromptAsync(contentType, request.PromptTemplate.Trim(), actor, ct);
            return Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }

    private static bool IsAllowedContentType(string contentType)
        => AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase);
}
