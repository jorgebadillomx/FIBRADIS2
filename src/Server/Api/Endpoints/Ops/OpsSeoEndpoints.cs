using System.Security.Claims;
using System.Text.Json;
using Application.Auth;
using Application.Catalog;
using Application.News;
using Application.Seo;
using Api.Seo;
using Domain.Seo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharedApiContracts.Seo;

namespace Api.Endpoints.Ops;

public static class OpsSeoEndpoints
{
    private const int MaxTitleLength = 120;
    private const int MaxDescriptionLength = 160;
    private const int MaxCanonicalPathLength = 256;
    private const int MaxOgImageUrlLength = 512;
    private const int MaxShortFieldLength = 32; // og_type / twitter_card (nvarchar(32))

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

            // robots solo se procesa si viene en el request (edición de robots de 12-11). Otros
            // campos pueden editarse sin tocar robots.
            string? normalizedRobotsDirectives = null;
            if (request.RobotsDirectives is not null)
            {
                if (!SeoRobotsDirectives.TryNormalize(request.RobotsDirectives, out var normalized, out var robotsErrors))
                    return Results.ValidationProblem(robotsErrors);
                normalizedRobotsDirectives = normalized;
            }

            var logger = loggerFactory.CreateLogger("OpsSeoEndpoints");
            var current = await repo.GetByIdAsync(id, ct);
            if (current is null)
                return Results.NotFound();

            ApplyOverrides(current, request, normalizedRobotsDirectives);
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

        // Backfill idempotente (AC-7): crea filas SeoMetadata faltantes para páginas fijas, fibras
        // activas y las últimas 100 noticias por CapturedAt. Re-ejecutarlo no duplica ni pisa
        // overrides (salta las claves ya existentes). Devuelve conteo por tipo.
        group.MapPost("/backfill", async (
            ISeoMetadataRepository seoRepo,
            ISeoDefaultsBuilder seoDefaults,
            ISpaMetadataProvider spaProvider,
            IFibraRepository fibraRepo,
            INewsRepository newsRepo,
            IConfiguration config,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var baseUrl = config["App:BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return Results.Problem(
                    "App:BaseUrl no está configurado; el backfill no puede generar URLs canónicas absolutas.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            baseUrl = baseUrl.TrimEnd('/');
            var logger = loggerFactory.CreateLogger("OpsSeoBackfill");
            var now = DateTimeOffset.UtcNow;
            var staticPages = 0;
            var fibras = 0;
            var news = 0;

            // ── Páginas fijas ──
            foreach (var path in spaProvider.KnownPaths)
            {
                var entityKey = path;
                if (await seoRepo.ExistsAsync(SeoPageType.StaticPage, entityKey, ct))
                    continue;

                try
                {
                    // GetMetaForPathAsync compone JSON-LD leyendo OperationalConfig/EditorialPage;
                    // un fallo en una página no debe abortar el backfill completo (se salta y sigue).
                    var meta = await spaProvider.GetMetaForPathAsync(path, ct);
                    if (meta is null)
                        continue;

                    var metadata = seoDefaults.BuildStaticPage(
                        SeoPageType.StaticPage, entityKey, meta.Title, meta.Description,
                        meta.CanonicalPath, meta.JsonLd, baseUrl, now);
                    await seoRepo.UpsertAsync(metadata, overrideMode: false, ct);
                    staticPages++;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Backfill SEO falló para la página fija {Path}; se omite", path);
                }
            }

            // ── Fibras activas ──
            foreach (var fibra in await fibraRepo.GetAllActiveAsync(ct))
            {
                var entityKey = fibra.Ticker.Trim().ToUpperInvariant();
                if (await seoRepo.ExistsAsync(SeoPageType.Fibra, entityKey, ct))
                    continue;

                try
                {
                    await seoRepo.UpsertAsync(seoDefaults.BuildFibra(fibra, baseUrl, now), overrideMode: false, ct);
                    fibras++;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Backfill SEO falló para la FIBRA {Ticker}; se omite", entityKey);
                }
            }

            // ── Últimas 100 noticias por CapturedAt ──
            foreach (var article in await newsRepo.GetLatestByCapturedAtAsync(100, ct))
            {
                var entityKey = article.Slug ?? article.Id.ToString();
                if (await seoRepo.ExistsAsync(SeoPageType.News, entityKey, ct))
                    continue;

                try
                {
                    await seoRepo.UpsertAsync(seoDefaults.BuildNews(article, baseUrl, now), overrideMode: false, ct);
                    news++;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Backfill SEO falló para la noticia {EntityKey}; se omite", entityKey);
                }
            }

            logger.LogInformation(
                "Ops SEO backfill creó {StaticPages} páginas fijas, {Fibras} fibras y {News} noticias",
                staticPages, fibras, news);

            return Results.Ok(new SeoBackfillResultDto(staticPages, fibras, news));
        })
        .Produces<SeoBackfillResultDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }

    // Aplica solo los campos provistos (no null) y marca su flag de override. og:title sigue la
    // regla "og:title == title": si se edita Title, OgTitle se alinea y también se marca override.
    private static void ApplyOverrides(SeoMetadata current, UpdateSeoMetadataRequest request, string? normalizedRobotsDirectives)
    {
        if (request.Title is not null)
        {
            current.Title = request.Title.Trim();
            current.TitleIsOverridden = true;
            current.OgTitle = current.Title;
            current.OgTitleIsOverridden = true;
        }

        if (request.MetaDescription is not null)
        {
            current.MetaDescription = request.MetaDescription.Trim();
            current.MetaDescriptionIsOverridden = true;
            current.OgDescription = current.MetaDescription;
            current.OgDescriptionIsOverridden = true;
        }

        if (request.CanonicalPath is not null)
        {
            current.CanonicalPath = request.CanonicalPath.Trim();
            current.CanonicalPathIsOverridden = true;
        }

        if (request.OgImageUrl is not null)
        {
            current.OgImageUrl = request.OgImageUrl.Trim();
            current.OgImageUrlIsOverridden = true;
        }

        if (request.OgType is not null)
        {
            current.OgType = request.OgType.Trim();
            current.OgTypeIsOverridden = true;
        }

        if (request.TwitterCard is not null)
        {
            current.TwitterCard = request.TwitterCard.Trim();
            current.TwitterCardIsOverridden = true;
        }

        if (request.JsonLd is not null)
        {
            current.JsonLd = string.IsNullOrWhiteSpace(request.JsonLd) ? null : request.JsonLd.Trim();
            current.JsonLdIsOverridden = true;
        }

        if (normalizedRobotsDirectives is not null)
        {
            current.RobotsDirectives = normalizedRobotsDirectives;
            current.RobotsDirectivesIsOverridden = true;
        }

        // IsActive NO se edita desde este PUT: el listado solo muestra filas activas (decisión de
        // 12-11), así que desactivar una fila la volvería inalcanzable para reactivarla y, además,
        // anularía silenciosamente los overrides editados (el middleware cae al fallback). La
        // (des)activación se maneja por seed/migración; reintroducirla aquí requiere que el listado
        // soporte filas inactivas primero.
    }

    private static Dictionary<string, string[]> ValidateRequest(UpdateSeoMetadataRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        // robots: el techo 256 lo valida también TryNormalize; aquí se mantiene por compatibilidad.
        if (request.RobotsDirectives?.Trim() is { Length: > 256 })
            errors["robotsDirectives"] = ["El campo robotsDirectives no puede superar 256 caracteres."];

        // Title: campo requerido (columna IsRequired). Si se provee, no puede quedar vacío tras trim
        // (marcaría override con "" y la regeneración nunca lo repondría → <title> vacío). Techo 120.
        if (request.Title is not null)
        {
            var title = request.Title.Trim();
            if (title.Length == 0)
                errors["title"] = ["El campo title no puede quedar vacío."];
            else if (title.Length > MaxTitleLength)
                errors["title"] = [$"El campo title no puede superar {MaxTitleLength} caracteres."];
        }

        // MetaDescription: si se provee, no puede quedar vacía tras trim. Techo 160 duro (400); el
        // piso 120 es solo objetivo de los defaults auto-generados (warning no bloqueante en edición).
        if (request.MetaDescription is not null)
        {
            var description = request.MetaDescription.Trim();
            if (description.Length == 0)
                errors["metaDescription"] = ["El campo metaDescription no puede quedar vacío."];
            else if (description.Length > MaxDescriptionLength)
                errors["metaDescription"] = [$"El campo metaDescription no puede superar {MaxDescriptionLength} caracteres."];
        }

        // CanonicalPath: ruta relativa (el dominio lo antepone App:BaseUrl) — debe empezar con '/' y
        // respetar el techo de columna nvarchar(256) (de lo contrario el upsert lanzaría 500).
        if (!string.IsNullOrWhiteSpace(request.CanonicalPath))
        {
            var canonical = request.CanonicalPath.Trim();
            if (!canonical.StartsWith('/'))
                errors["canonicalPath"] = ["canonicalPath debe ser una ruta relativa que comience con '/'."];
            else if (canonical.Length > MaxCanonicalPathLength)
                errors["canonicalPath"] = [$"canonicalPath no puede superar {MaxCanonicalPathLength} caracteres."];
        }

        // OgImageUrl: URL absoluta http/https (guard SSRF) o ruta relativa, techo nvarchar(512).
        if (!string.IsNullOrWhiteSpace(request.OgImageUrl))
        {
            var ogImageUrl = request.OgImageUrl.Trim();
            if (!IsValidImageUrl(ogImageUrl))
                errors["ogImageUrl"] = ["ogImageUrl debe ser una URL http/https o una ruta relativa que comience con '/'."];
            else if (ogImageUrl.Length > MaxOgImageUrlLength)
                errors["ogImageUrl"] = [$"ogImageUrl no puede superar {MaxOgImageUrlLength} caracteres."];
        }

        // OgType / TwitterCard: columnas nvarchar(32); sin este guard un valor largo provocaría
        // DbUpdateException (500) en lugar de un 400 validado.
        if (request.OgType?.Trim() is { Length: > MaxShortFieldLength })
            errors["ogType"] = [$"El campo ogType no puede superar {MaxShortFieldLength} caracteres."];

        if (request.TwitterCard?.Trim() is { Length: > MaxShortFieldLength })
            errors["twitterCard"] = [$"El campo twitterCard no puede superar {MaxShortFieldLength} caracteres."];

        // JsonLd: si viene no vacío, debe ser JSON parseable.
        if (!string.IsNullOrWhiteSpace(request.JsonLd) && !IsParseableJson(request.JsonLd))
            errors["jsonLd"] = ["jsonLd debe ser un JSON válido."];

        return errors;
    }

    private static bool IsValidImageUrl(string value)
    {
        if (value.StartsWith('/'))
            return true;

        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static bool IsParseableJson(string value)
    {
        try
        {
            using var _ = JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
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
        metadata.UpdatedBy,
        metadata.OgTitle,
        metadata.OgDescription,
        metadata.OgType,
        metadata.OgImageUrl,
        metadata.OgLocale,
        metadata.TwitterCard,
        metadata.JsonLd,
        metadata.TitleIsOverridden,
        metadata.MetaDescriptionIsOverridden,
        metadata.CanonicalPathIsOverridden,
        metadata.OgImageUrlIsOverridden,
        metadata.OgTypeIsOverridden,
        metadata.TwitterCardIsOverridden,
        metadata.JsonLdIsOverridden);

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
