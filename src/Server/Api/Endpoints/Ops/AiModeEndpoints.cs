using System.Security.Claims;
using Application.News;
using Domain.News;
using Infrastructure.Integrations.Articles;
using Microsoft.Extensions.Logging;
using SharedApiContracts.Common;
using SharedApiContracts.News;

namespace Api.Endpoints.Ops;

public static class AiModeEndpoints
{
    private static readonly IReadOnlySet<string> AllowedNewsModels = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "gemini-2.5-flash",
        "gemini-2.5-pro",
    };

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
                config.NewsModel,
                config.UpdatedAt,
                config.UpdatedBy,
                config.PreviousMode?.ToString()));
        })
        .Produces<AiModeDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        configGroup.MapPut("/", async (
            UpdateAiModeRequest request,
            IAiModeRepository repo,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (request.Mode is null && request.NewsModel is null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["body"] = ["Se debe proporcionar al menos `mode` o `newsModel`."],
                });
            }

            AiMode? parsedMode = null;
            if (request.Mode is not null)
            {
                if (!Enum.TryParse<AiMode>(request.Mode, ignoreCase: true, out var mode) || !Enum.IsDefined(mode))
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["mode"] = ["Valor inválido. Use 'Off' o 'On'."],
                    });
                }
                parsedMode = mode;
            }

            if (request.NewsModel is not null && !AllowedNewsModels.Contains(request.NewsModel))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["newsModel"] = ["Modelo no permitido. Valores válidos: gemini-2.5-flash, gemini-2.5-pro."],
                });
            }

            var actor = ctx.User.Identity?.Name
                ?? ctx.User.FindFirstValue(ClaimTypes.Email)
                ?? ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "unknown";

            await repo.UpdateConfigAsync(parsedMode, request.NewsModel?.ToLowerInvariant(), actor, ct);
            return Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        var newsGroup = app.MapGroup("/api/v1/ops/news")
            .RequireAuthorization("AdminOps")
            .WithTags("AI");

        newsGroup.MapGet("/", async (
            INewsRepository newsRepo,
            CancellationToken ct,
            int page = 1,
            int pageSize = 20,
            string? search = null,
            bool? hasAiSummary = null) =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var trimmedSearch = search?.Trim();
            if (trimmedSearch?.Length > 200)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["search"] = ["El parámetro search no puede superar los 200 caracteres."],
                });
            }

            var (items, total) = await newsRepo.GetPagedForOpsAsync(page, pageSize, trimmedSearch, hasAiSummary, ct);
            var dtos = items.Select(a => new OpsNewsArticleDto(
                a.Id,
                a.Title,
                a.Source,
                a.PublishedAt,
                a.Url,
                a.Status.ToString(),
                a.BodyText?.Length,
                a.BodyText is not null ? a.BodyText[..Math.Min(200, a.BodyText.Length)] : null,
                a.AiSummary is not null,
                a.AiSummary is not null ? a.AiSummary[..Math.Min(300, a.AiSummary.Length)] : null)).ToList();
            return Results.Ok(new PagedResult<OpsNewsArticleDto>(dtos, page, pageSize, total));
        })
        .Produces<PagedResult<OpsNewsArticleDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        newsGroup.MapGet("/{articleId:guid}", async (
            Guid articleId,
            INewsRepository newsRepo,
            CancellationToken ct) =>
        {
            var article = await newsRepo.GetByIdAsync(articleId, ct);
            if (article is null)
                return Results.NotFound();
            return Results.Ok(new OpsNewsBodyDto(article.Id, article.BodyText, article.AiSummary));
        })
        .Produces<OpsNewsBodyDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        newsGroup.MapPut("/{articleId:guid}/body-text", async (
            Guid articleId,
            UpdateBodyTextRequest request,
            INewsRepository newsRepo,
            CancellationToken ct) =>
        {
            var article = await newsRepo.GetByIdAsync(articleId, ct);
            if (article is null)
                return Results.NotFound();


            var bodyText = string.IsNullOrWhiteSpace(request.BodyText) ? null : request.BodyText.Trim();
            await newsRepo.UpdateBodyTextAsync(articleId, bodyText, ct);
            return Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        newsGroup.MapPost("/{articleId:guid}/ai-summary", async (
            Guid articleId,
            INewsRepository newsRepo,
            IArticleContentScraper articleContentScraper,
            IAiSummaryService summaryService,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("AiModeEndpoints");
            var article = await newsRepo.GetByIdAsync(articleId, ct);
            if (article is null)
                return Results.NotFound();

            try
            {
                var bodyText = article.BodyText;
                if (NeedsBodyRefresh(bodyText) && !string.IsNullOrWhiteSpace(article.Url))
                {
                    bodyText = await articleContentScraper.TryGetArticleTextAsync(article.Url, ct);
                    bodyText = NormalizeBodyText(bodyText);
                    if (!string.IsNullOrWhiteSpace(bodyText))
                        await newsRepo.UpdateBodyTextAsync(articleId, bodyText, ct);
                }

                var summary = await summaryService.GenerateSummaryAsync(article.Title, article.Snippet, bodyText, AiContentType.News, ct);

                // P4: null indica proveedor no configurado → 503
                if (summary is null)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status503ServiceUnavailable,
                        detail: "El servicio de IA no está configurado. Verifique la configuración del proveedor activo (Gemini:ApiKey o DeepSeek:ApiKey).");
                }

                await newsRepo.UpdateSummaryAsync(articleId, summary, NewsArticleStatus.Processed, ct);
                return Results.NoContent();
            }
            catch (AiProviderConfigurationException ex)
            {
                logger.LogError(ex, "AI provider configuration error while generating AI summary for news article {ArticleId}", articleId);

                return Results.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    detail: ex.Message);
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

    private static bool NeedsBodyRefresh(string? bodyText)
        => string.IsNullOrWhiteSpace(bodyText)
            || bodyText.Length < 200
            || string.Equals(bodyText.Trim(), "Google News", StringComparison.OrdinalIgnoreCase);

    private static string? NormalizeBodyText(string? bodyText)
    {
        var normalized = NewsTextNormalizer.Normalize(bodyText);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}

public static class AiProviderEndpoints
{
    private static readonly IReadOnlyList<AiProviderOptionDto> AvailableProviders =
    [
        new("Gemini", ["gemini-2.5-flash", "gemini-2.5-pro"]),
        new("DeepSeek", ["deepseek-v4-flash", "deepseek-v4-pro"]),
    ];

    public static IEndpointRouteBuilder MapAiProvider(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ops/ai-provider")
            .RequireAuthorization("AdminOps")
            .WithTags("AI");

        group.MapGet("/", async (
            IAiProviderConfigRepository repo,
            CancellationToken ct) =>
        {
            var config = await repo.GetConfigAsync(ct);
            return Results.Ok(new AiProviderConfigDto(
                config.Provider.ToString(),
                config.ModelId,
                config.UpdatedAt,
                config.UpdatedBy,
                AvailableProviders));
        })
        .Produces<AiProviderConfigDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPut("/", async (
            SetAiProviderRequest request,
            IAiProviderConfigRepository repo,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<AiProvider>(request.Provider, ignoreCase: true, out var provider) || !Enum.IsDefined(provider))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["provider"] = [$"Valor inválido. Use: {string.Join(", ", Enum.GetNames<AiProvider>())}."],
                });
            }

            var option = AvailableProviders.FirstOrDefault(p =>
                string.Equals(p.Provider, request.Provider, StringComparison.OrdinalIgnoreCase));

            if (option is null || !option.Models.Contains(request.ModelId, StringComparer.OrdinalIgnoreCase))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["modelId"] = [$"Modelo inválido para {provider}. Use: {string.Join(", ", option?.Models ?? [])}."],
                });
            }

            var actor = ctx.User.Identity?.Name
                ?? ctx.User.FindFirstValue(ClaimTypes.Email)
                ?? ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "unknown";

            await repo.SetProviderAsync(provider, request.ModelId, actor, ct);
            return Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }
}

public sealed record SetAiProviderRequest(string Provider, string ModelId);

public sealed record UpdateAiModeRequest(string? Mode, string? NewsModel);
