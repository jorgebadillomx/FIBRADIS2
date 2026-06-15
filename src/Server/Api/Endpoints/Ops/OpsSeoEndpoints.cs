using System.Security.Claims;
using Application.Auth;
using Application.Seo;
using Api.Seo;
using Domain.Seo;
using Microsoft.Extensions.Logging;
using SharedApiContracts.Seo;

namespace Api.Endpoints.Ops;

public static class OpsSeoEndpoints
{
    public static IEndpointRouteBuilder MapOpsSeo(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ops/seo")
            .RequireAuthorization("AdminOps")
            .WithTags("OpsSeo");

        group.MapGet("", async (
            string? pageType,
            string? search,
            ISeoMetadataRepository repo,
            CancellationToken ct) =>
        {
            // pageType ausente/vacío = "Todos" (sin filtro): el repositorio y SeoMetadataQuery
            // ya soportan PageType null. Solo se valida cuando llega un valor no vacío.
            SeoPageType? parsedPageType = null;
            if (!string.IsNullOrWhiteSpace(pageType))
            {
                if (!TryParsePageType(pageType, out var parsed))
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["pageType"] = ["pageType debe ser un valor válido de SeoPageType."],
                    });
                }

                parsedPageType = parsed;
            }

            var rows = await repo.GetAllAsync(new SeoMetadataQuery(parsedPageType, search), ct);
            // El editor solo lista filas activas: el override de robots de una fila inactiva se
            // ignora tanto en el <meta> (los middlewares reconstruyen desde defaults) como en el
            // sitemap (solo excluye IsActive && noindex), así que listarlas engañaría al operador.
            return Results.Ok(rows.Where(row => row.IsActive).Select(ToDto).ToList());
        })
        .Produces<IReadOnlyList<SeoMetadataDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/{id:guid}", async (
            Guid id,
            ISeoMetadataRepository repo,
            CancellationToken ct) =>
        {
            var row = await repo.GetByIdAsync(id, ct);
            return row is null ? Results.NotFound() : Results.Ok(ToDto(row));
        })
        .Produces<SeoMetadataDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateSeoMetadataRequest request,
            ISeoMetadataRepository repo,
            IEmailEncryptor emailEncryptor,
            ILoggerFactory loggerFactory,
            SeoSitemapCacheState sitemapCacheState,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var validation = ValidateRequest(request);
            if (validation.Count > 0)
                return Results.ValidationProblem(validation);

            if (!SeoRobotsDirectives.TryNormalize(request.RobotsDirectives, out var normalizedRobotsDirectives, out var robotsErrors))
                return Results.ValidationProblem(robotsErrors);

            var logger = loggerFactory.CreateLogger("OpsSeoEndpoints");
            var current = await repo.GetByIdAsync(id, ct);
            if (current is null)
                return Results.NotFound();

            current.RobotsDirectives = normalizedRobotsDirectives;
            current.RobotsDirectivesIsOverridden = true;
            current.UpdatedAt = DateTimeOffset.UtcNow;
            current.UpdatedBy = GetActor(ctx, emailEncryptor, logger);

            await repo.UpsertAsync(current, overrideMode: true, ct);
            sitemapCacheState.Invalidate();

            logger.LogInformation(
                "Ops {Action} SEO metadata {SeoMetadataId} by {Actor} at {Timestamp}",
                "UPDATE",
                current.Id,
                current.UpdatedBy,
                current.UpdatedAt);

            return Results.Ok(ToDto(current));
        })
        .Produces<SeoMetadataDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }

    private static Dictionary<string, string[]> ValidateRequest(UpdateSeoMetadataRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var value = request.RobotsDirectives?.Trim();

        if (value is { Length: > 256 })
            errors["robotsDirectives"] = ["El campo robotsDirectives no puede superar 256 caracteres."];

        return errors;
    }

    private static bool TryParsePageType(string? value, out SeoPageType pageType)
    {
        pageType = default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        return Enum.TryParse(value.Trim(), ignoreCase: true, out pageType) &&
               Enum.IsDefined(pageType);
    }

    private static SeoMetadataDto ToDto(SeoMetadata metadata) => new(
        metadata.Id,
        metadata.PageType.ToString(),
        metadata.EntityKey,
        metadata.Title,
        metadata.MetaDescription,
        metadata.CanonicalPath,
        metadata.RobotsDirectives,
        metadata.RobotsDirectivesIsOverridden,
        metadata.IsActive,
        metadata.UpdatedAt,
        metadata.UpdatedBy);

    private static string GetActor(HttpContext ctx, IEmailEncryptor emailEncryptor, ILogger logger)
    {
        var actor = ctx.User.Identity?.Name
            ?? ctx.User.FindFirstValue(ClaimTypes.Email)
            ?? ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (actor is null)
        {
            logger.LogWarning("GetActor: no identity claim found in JWT; using 'unknown'");
            return "unknown";
        }

        return emailEncryptor.Decrypt(actor);
    }
}
