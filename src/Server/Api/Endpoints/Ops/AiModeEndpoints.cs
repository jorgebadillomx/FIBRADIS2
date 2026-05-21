using System.Security.Claims;
using Application.News;
using Domain.News;
using Microsoft.Extensions.Logging;
using SharedApiContracts.News;

namespace Api.Endpoints.Ops;

public static class AiModeEndpoints
{
    public static IEndpointRouteBuilder MapAiMode(this IEndpointRouteBuilder app)
    {
        var configGroup = app.MapGroup("/api/v1/ops/ai-mode")
            .RequireAuthorization("AdminOps")
            .WithTags("AI");

        configGroup.MapGet("/", async (
            IAiModeRepository repo,
            CancellationToken ct) =>
        {
            var config = await repo.GetConfigAsync(ct);
            return Results.Ok(new AiModeDto(
                config.Mode.ToString(),
                config.UpdatedAt,
                config.UpdatedBy,
                config.PreviousMode?.ToString()));
        })
        .Produces<AiModeDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        configGroup.MapPut("/", async (
            SetAiModeRequest request,
            IAiModeRepository repo,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<AiMode>(request.Mode, ignoreCase: true, out var mode) || !Enum.IsDefined(mode))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["mode"] = ["Valor inválido. Use 'Off' o 'On'."],
                });
            }

            var actor = ctx.User.Identity?.Name
                ?? ctx.User.FindFirstValue(ClaimTypes.Email)
                ?? ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "unknown";

            await repo.SetModeAsync(mode, actor, ct);
            return Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        var newsGroup = app.MapGroup("/api/v1/ops/news")
            .RequireAuthorization("AdminOps")
            .WithTags("AI");

        newsGroup.MapPost("/{articleId:guid}/ai-summary", async (
            Guid articleId,
            INewsRepository newsRepo,
            IAiSummaryService summaryService,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("AiModeEndpoints");
            var article = await newsRepo.GetByIdAsync(articleId, ct);
            if (article is null)
                return Results.NotFound();

            // P2: idempotencia — no reprocesar artículos ya procesados
            if (article.Status == NewsArticleStatus.Processed)
                return Results.NoContent();

            try
            {
                var summary = await summaryService.GenerateSummaryAsync(article.Title, article.Snippet, AiContentType.News, ct);

                // P4: null indica proveedor no configurado → 503
                if (summary is null)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status503ServiceUnavailable,
                        detail: "El servicio de IA no está configurado. Verifique la configuración de Gemini:ApiKey.");
                }

                await newsRepo.UpdateSummaryAsync(articleId, summary, NewsArticleStatus.Processed, ct);
                return Results.NoContent();
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // P5: excepción en proveedor AI → marcar Partial y devolver 502
                logger.LogError(ex, "Failed to generate AI summary for news article {ArticleId}", articleId);

                try
                {
                    await newsRepo.UpdateSummaryAsync(articleId, null, NewsArticleStatus.Partial, CancellationToken.None);
                }
                catch (Exception updateEx)
                {
                    logger.LogError(
                        updateEx,
                        "Failed to mark news article {ArticleId} as partial after AI summary failure",
                        articleId);
                }

                return Results.Problem(
                    statusCode: StatusCodes.Status502BadGateway,
                    detail: "El proveedor de IA no está disponible. El artículo fue marcado como Partial.");
            }
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status503ServiceUnavailable)
        .Produces(StatusCodes.Status502BadGateway)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }
}

public sealed record SetAiModeRequest(string Mode);
