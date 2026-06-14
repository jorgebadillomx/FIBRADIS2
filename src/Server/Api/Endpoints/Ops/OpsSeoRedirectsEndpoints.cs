using System.Security.Claims;
using Application.Auth;
using Application.Seo;
using Domain.Seo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SharedApiContracts.Seo;

namespace Api.Endpoints.Ops;

public static class OpsSeoRedirectsEndpoints
{
    private const int MaxPathLength = 256;
    private const int MaxNotesLength = 1000;
    private const string CacheKey = "seo-url-redirects-active";

    public static IEndpointRouteBuilder MapOpsSeoRedirects(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ops/seo/redirects")
            .RequireAuthorization("AdminOps")
            .WithTags("OpsSeo");

        group.MapGet("", async (
            IRedirectRepository repo,
            CancellationToken ct) =>
        {
            var items = await repo.GetAllAsync(ct);
            return Results.Ok(items.Select(ToDto).ToList());
        })
        .Produces<IReadOnlyList<UrlRedirectDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("", async (
            UpsertUrlRedirectRequest request,
            IRedirectRepository repo,
            IMemoryCache cache,
            IEmailEncryptor emailEncryptor,
            ILoggerFactory loggerFactory,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("OpsSeoRedirectsEndpoints");
            var validation = ValidateRequest(request, null);
            if (validation.Count > 0)
                return Results.ValidationProblem(validation);

            var normalizedFromPath = UrlRedirectPath.Normalize(request.FromPath);
            if (await repo.GetByFromPathAsync(normalizedFromPath, ct) is not null)
            {
                return Results.Problem(
                    title: "Redirect duplicado",
                    detail: "Ya existe un redirect con el mismo FromPath.",
                    statusCode: StatusCodes.Status409Conflict,
                    extensions: new Dictionary<string, object?> { ["domainCode"] = "REDIRECT_ALREADY_EXISTS" });
            }

            if (await HasReverseLoopAsync(repo, normalizedFromPath, UrlRedirectPath.Normalize(request.ToPath), null, ct))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["toPath"] = ["ToPath generaría un loop con un redirect activo existente."],
                });
            }

            var actor = GetActor(ctx, emailEncryptor, logger);
            var timestamp = DateTimeOffset.UtcNow;
            var redirect = new UrlRedirect
            {
                Id = Guid.NewGuid(),
                FromPath = normalizedFromPath,
                ToPath = UrlRedirectPath.Normalize(request.ToPath),
                StatusCode = request.StatusCode,
                IsActive = request.IsActive,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                CreatedAt = timestamp,
                CreatedBy = actor,
                UpdatedAt = timestamp,
                UpdatedBy = actor,
            };

            try
            {
                await repo.AddAsync(redirect, ct);
            }
            catch (DbUpdateException)
            {
                return Results.Problem(
                    title: "Redirect duplicado",
                    detail: "Ya existe un redirect con el mismo FromPath.",
                    statusCode: StatusCodes.Status409Conflict,
                    extensions: new Dictionary<string, object?> { ["domainCode"] = "REDIRECT_ALREADY_EXISTS" });
            }

            cache.Remove(CacheKey);

            logger.LogInformation(
                "Ops {Action} URL redirect {RedirectId} by {Actor} at {Timestamp}",
                "CREATE",
                redirect.Id,
                redirect.UpdatedBy,
                redirect.UpdatedAt);

            return Results.Created($"/api/v1/ops/seo/redirects/{redirect.Id}", ToDto(redirect));
        })
        .Produces<UrlRedirectDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpsertUrlRedirectRequest request,
            IRedirectRepository repo,
            IMemoryCache cache,
            IEmailEncryptor emailEncryptor,
            ILoggerFactory loggerFactory,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("OpsSeoRedirectsEndpoints");
            var current = await repo.GetByIdAsync(id, ct);
            if (current is null)
                return Results.NotFound();

            var validation = ValidateRequest(request, current.Id);
            if (validation.Count > 0)
                return Results.ValidationProblem(validation);

            var normalizedFromPath = UrlRedirectPath.Normalize(request.FromPath);
            var existing = await repo.GetByFromPathAsync(normalizedFromPath, ct);
            if (existing is not null && existing.Id != current.Id)
            {
                return Results.Problem(
                    title: "Redirect duplicado",
                    detail: "Ya existe un redirect con el mismo FromPath.",
                    statusCode: StatusCodes.Status409Conflict,
                    extensions: new Dictionary<string, object?> { ["domainCode"] = "REDIRECT_ALREADY_EXISTS" });
            }

            if (await HasReverseLoopAsync(repo, normalizedFromPath, UrlRedirectPath.Normalize(request.ToPath), current.Id, ct))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["toPath"] = ["ToPath generaría un loop con un redirect activo existente."],
                });
            }

            current.FromPath = normalizedFromPath;
            current.ToPath = UrlRedirectPath.Normalize(request.ToPath);
            current.StatusCode = request.StatusCode;
            current.IsActive = request.IsActive;
            current.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
            current.UpdatedAt = DateTimeOffset.UtcNow;
            current.UpdatedBy = GetActor(ctx, emailEncryptor, logger);

            try
            {
                await repo.UpdateAsync(current, ct);
            }
            catch (DbUpdateException)
            {
                return Results.Problem(
                    title: "Redirect duplicado",
                    detail: "Ya existe un redirect con el mismo FromPath.",
                    statusCode: StatusCodes.Status409Conflict,
                    extensions: new Dictionary<string, object?> { ["domainCode"] = "REDIRECT_ALREADY_EXISTS" });
            }

            cache.Remove(CacheKey);

            logger.LogInformation(
                "Ops {Action} URL redirect {RedirectId} by {Actor} at {Timestamp}",
                "UPDATE",
                current.Id,
                current.UpdatedBy,
                current.UpdatedAt);

            return Results.Ok(ToDto(current));
        })
        .Produces<UrlRedirectDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/deactivate", async (
            Guid id,
            IRedirectRepository repo,
            IMemoryCache cache,
            IEmailEncryptor emailEncryptor,
            ILoggerFactory loggerFactory,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("OpsSeoRedirectsEndpoints");
            var current = await repo.GetByIdAsync(id, ct);
            if (current is null)
                return Results.NotFound();

            if (current.IsActive)
            {
                current.IsActive = false;
                current.UpdatedAt = DateTimeOffset.UtcNow;
                current.UpdatedBy = GetActor(ctx, emailEncryptor, logger);
                await repo.UpdateAsync(current, ct);
                cache.Remove(CacheKey);

                logger.LogInformation(
                    "Ops {Action} URL redirect {RedirectId} by {Actor} at {Timestamp}",
                    "DEACTIVATE",
                    current.Id,
                    current.UpdatedBy,
                    current.UpdatedAt);
            }

            return Results.Ok(ToDto(current));
        })
        .Produces<UrlRedirectDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/{id:guid}/activate", async (
            Guid id,
            IRedirectRepository repo,
            IMemoryCache cache,
            IEmailEncryptor emailEncryptor,
            ILoggerFactory loggerFactory,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("OpsSeoRedirectsEndpoints");
            var current = await repo.GetByIdAsync(id, ct);
            if (current is null)
                return Results.NotFound();

            if (!current.IsActive)
            {
                current.IsActive = true;
                current.UpdatedAt = DateTimeOffset.UtcNow;
                current.UpdatedBy = GetActor(ctx, emailEncryptor, logger);
                await repo.UpdateAsync(current, ct);
                cache.Remove(CacheKey);

                logger.LogInformation(
                    "Ops {Action} URL redirect {RedirectId} by {Actor} at {Timestamp}",
                    "ACTIVATE",
                    current.Id,
                    current.UpdatedBy,
                    current.UpdatedAt);
            }

            return Results.Ok(ToDto(current));
        })
        .Produces<UrlRedirectDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }

    private static Dictionary<string, string[]> ValidateRequest(UpsertUrlRedirectRequest request, Guid? currentId)
    {
        var errors = new Dictionary<string, string[]>();

        AddRequiredPath(errors, "fromPath", request.FromPath);
        AddRequiredPath(errors, "toPath", request.ToPath);

        if (request.FromPath?.Trim().Length > MaxPathLength)
            errors["fromPath"] = [$"El campo fromPath no puede superar {MaxPathLength} caracteres."];

        if (request.ToPath?.Trim().Length > MaxPathLength)
            errors["toPath"] = [$"El campo toPath no puede superar {MaxPathLength} caracteres."];

        if (request.Notes is not null && request.Notes.Length > MaxNotesLength)
            errors["notes"] = [$"El campo notes no puede superar {MaxNotesLength} caracteres."];

        if (request.StatusCode is not 301 and not 302)
            errors["statusCode"] = ["StatusCode debe ser 301 o 302."];

        var normalizedFromPath = request.FromPath is not null ? UrlRedirectPath.Normalize(request.FromPath) : string.Empty;
        var normalizedToPath = request.ToPath is not null ? UrlRedirectPath.Normalize(request.ToPath) : string.Empty;

        if (!UrlRedirectPath.IsInternalPath(request.FromPath ?? string.Empty))
            errors["fromPath"] = ["FromPath debe comenzar con '/' y ser una ruta interna."];

        if (!UrlRedirectPath.IsInternalPath(request.ToPath ?? string.Empty))
            errors["toPath"] = ["ToPath debe comenzar con '/' y ser una ruta interna."];

        if (normalizedFromPath.Length > 0 && UrlRedirectPath.IsReservedSource(normalizedFromPath))
            errors["fromPath"] = ["FromPath no puede colisionar con rutas reservadas como /api/, /ops/, /fibras/ o /hangfire/."];

        if (normalizedFromPath.Length > 0 && string.Equals(normalizedFromPath, normalizedToPath, StringComparison.OrdinalIgnoreCase))
            errors["toPath"] = ["FromPath y ToPath no pueden ser iguales."];

        return errors;
    }

    private static void AddRequiredPath(Dictionary<string, string[]> errors, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors[field] = [$"El campo {field} es requerido."];
    }

    private static UrlRedirectDto ToDto(UrlRedirect item) => new(
        item.Id,
        item.FromPath,
        item.ToPath,
        item.StatusCode,
        item.IsActive,
        item.Notes,
        item.CreatedAt,
        item.CreatedBy,
        item.UpdatedAt,
        item.UpdatedBy);

    private static async Task<bool> HasReverseLoopAsync(
        IRedirectRepository repo,
        string normalizedFromPath,
        string normalizedToPath,
        Guid? currentId,
        CancellationToken ct)
    {
        var activeRedirects = await repo.GetActiveAsync(ct);
        return activeRedirects.Any(item =>
            (currentId is null || item.Id != currentId.Value) &&
            item.FromPath == normalizedToPath &&
            item.ToPath == normalizedFromPath);
    }

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
