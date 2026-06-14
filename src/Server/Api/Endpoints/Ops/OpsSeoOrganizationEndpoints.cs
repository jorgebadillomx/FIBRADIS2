using System.Security.Claims;
using System.Text.Json;
using Application.Auth;
using Application.Ops;
using Microsoft.Extensions.Logging;

namespace Api.Endpoints.Ops;

public static class OpsSeoOrganizationEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapOpsSeoOrganization(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ops/seo/organization")
            .RequireAuthorization("AdminOps")
            .WithTags("OpsSeo");

        group.MapGet("", async (
            IOperationalConfigRepository repo,
            CancellationToken ct) =>
        {
            var config = await repo.GetAsync(ct);
            return Results.Ok(new OrganizationSameAsDto(
                config.UpdatedAt,
                config.UpdatedBy,
                ParseSameAs(config.OrganizationSameAsJson)));
        })
        .Produces<OrganizationSameAsDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPut("", async (
            UpdateOrganizationSameAsRequest request,
            IOperationalConfigRepository repo,
            IEmailEncryptor emailEncryptor,
            ILoggerFactory loggerFactory,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var validation = Validate(request.SameAs);
            if (validation.Count > 0)
                return Results.ValidationProblem(validation);

            var logger = loggerFactory.CreateLogger("OpsSeoOrganizationEndpoints");
            var actor = GetActor(ctx, emailEncryptor, logger);
            var normalized = request.SameAs is { Count: > 0 }
                ? JsonSerializer.Serialize(Normalize(request.SameAs), JsonOptions)
                : null;

            await repo.UpdateOrganizationSameAsAsync(normalized, actor, ct);

            logger.LogInformation(
                "Ops {Action} organization sameAs by {Actor} at {Timestamp}",
                "UPDATE",
                actor,
                DateTimeOffset.UtcNow);

            var updated = await repo.GetAsync(ct);
            return Results.Ok(new OrganizationSameAsDto(
                updated.UpdatedAt,
                updated.UpdatedBy,
                ParseSameAs(updated.OrganizationSameAsJson)));
        })
        .Produces<OrganizationSameAsDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }

    private static Dictionary<string, string[]> Validate(IReadOnlyList<string>? sameAs)
    {
        var errors = new Dictionary<string, string[]>();
        var urls = sameAs ?? Array.Empty<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < urls.Count; index++)
        {
            var raw = urls[index]?.Trim();
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri) ||
                (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                 !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
            {
                errors[$"sameAs[{index}]"] = ["Cada URL debe ser absoluta y usar http o https."];
                continue;
            }

            if (!seen.Add(uri.ToString()))
                errors[$"sameAs[{index}]"] = ["No se permiten URLs duplicadas."];
        }

        return errors;
    }

    private static IReadOnlyList<string> Normalize(IReadOnlyList<string> sameAs)
        => sameAs
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static IReadOnlyList<string> ParseSameAs(string? sameAsJson)
    {
        if (string.IsNullOrWhiteSpace(sameAsJson))
            return Array.Empty<string>();

        try
        {
            var urls = JsonSerializer.Deserialize<string?[]>(sameAsJson, JsonOptions) ?? [];
            return urls
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Select(url => url!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
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

    private sealed record OrganizationSameAsDto(
        DateTimeOffset UpdatedAt,
        string? UpdatedBy,
        IReadOnlyList<string> SameAs);

    private sealed record UpdateOrganizationSameAsRequest(IReadOnlyList<string>? SameAs);
}
