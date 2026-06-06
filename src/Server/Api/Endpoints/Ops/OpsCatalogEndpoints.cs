using System.Security.Claims;
using Application.Auth;
using Application.Catalog;
using Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedApiContracts.Catalog;

namespace Api.Endpoints.Ops;

public static class OpsCatalogEndpoints
{
    private static readonly IReadOnlySet<string> AllowedCurrencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "MXN",
        "USD",
        "EUR",
        "UDI",
    };

    public static IEndpointRouteBuilder MapOpsCatalog(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ops/catalog")
            .RequireAuthorization("AdminOps")
            .WithTags("Catalog");

        group.MapGet("/", async (
            IFibraRepository repo,
            CancellationToken ct) =>
        {
            var fibras = await repo.GetAllAsync(ct);
            return Results.Ok(fibras.Select(ToDto).ToList());
        })
        .Produces<IReadOnlyList<FibraDetail>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/", async (
            CreateFibraRequest request,
            IFibraRepository repo,
            ILoggerFactory loggerFactory,
            IEmailEncryptor emailEncryptor,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("OpsCatalogEndpoints");
            var errors = ValidateCreateRequest(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var ticker = request.Ticker.Trim().ToUpperInvariant();
            var yahooTicker = request.YahooTicker.Trim();

            if (await repo.ExistsByTickerAsync(ticker, ct))
            {
                return Results.Problem(
                    title: "Ticker duplicado",
                    detail: "El ticker ya existe en el catálogo.",
                    statusCode: StatusCodes.Status409Conflict,
                    extensions: new Dictionary<string, object?> { ["domainCode"] = "TICKER_ALREADY_EXISTS" });
            }

            var timestamp = DateTimeOffset.UtcNow;
            var actor = GetActor(ctx, emailEncryptor, logger);
            var fibra = new Fibra
            {
                Id = Guid.NewGuid(),
                Ticker = ticker,
                YahooTicker = yahooTicker,
                FullName = request.FullName.Trim(),
                ShortName = request.ShortName.Trim(),
                Sector = request.Sector.Trim(),
                Market = request.Market.Trim(),
                Currency = request.Currency.Trim().ToUpperInvariant(),
                SiteUrl = NormalizeOptional(request.SiteUrl),
                InvestorUrl = NormalizeOptional(request.InvestorUrl),
                ReportsUrl = NormalizeOptional(request.ReportsUrl),
                NameVariants = NormalizeVariants(request.NameVariants),
                Description = NormalizeOptional(request.Description),
                State = FibraState.Active,
                CreatedAt = timestamp,
            };

            try
            {
                await repo.AddAsync(fibra, ct);
            }
            catch (DbUpdateException)
            {
                return Results.Problem(
                    title: "Ticker duplicado",
                    detail: "El ticker ya existe en el catálogo.",
                    statusCode: StatusCodes.Status409Conflict,
                    extensions: new Dictionary<string, object?> { ["domainCode"] = "TICKER_ALREADY_EXISTS" });
            }

            logger.LogInformation(
                "Ops {Action} FIBRA {Ticker} by {Actor} at {Timestamp}",
                "CREATE",
                fibra.Ticker,
                actor,
                timestamp);

            return Results.Created($"/api/v1/ops/catalog/{fibra.Ticker}", ToDto(fibra));
        })
        .Produces<FibraDetail>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{ticker}", async (
            string ticker,
            UpdateFibraRequest request,
            IFibraRepository repo,
            ILoggerFactory loggerFactory,
            IEmailEncryptor emailEncryptor,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            ticker = ticker.Trim().ToUpperInvariant();
            var logger = loggerFactory.CreateLogger("OpsCatalogEndpoints");
            var errors = ValidateUpdateRequest(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var fibra = await repo.GetByTickerAsync(ticker, ct);
            if (fibra is null)
            {
                return Results.NotFound();
            }

            fibra.YahooTicker = request.YahooTicker.Trim();
            fibra.FullName = request.FullName.Trim();
            fibra.ShortName = request.ShortName.Trim();
            fibra.Sector = request.Sector.Trim();
            fibra.Market = request.Market.Trim();
            fibra.Currency = request.Currency.Trim().ToUpperInvariant();
            fibra.SiteUrl = NormalizeOptional(request.SiteUrl);
            fibra.InvestorUrl = NormalizeOptional(request.InvestorUrl);
            fibra.ReportsUrl = NormalizeOptional(request.ReportsUrl);
            fibra.Description = NormalizeOptional(request.Description);

            if (request.NameVariants is not null)
            {
                fibra.NameVariants = NormalizeVariants(request.NameVariants);
            }

            await repo.UpdateAsync(fibra, ct);

            var timestamp = DateTimeOffset.UtcNow;
            logger.LogInformation(
                "Ops {Action} FIBRA {Ticker} by {Actor} at {Timestamp}",
                "UPDATE",
                fibra.Ticker,
                GetActor(ctx, emailEncryptor, logger),
                timestamp);

            return Results.Ok(ToDto(fibra));
        })
        .Produces<FibraDetail>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/{ticker}/deactivate", async (
            string ticker,
            IFibraRepository repo,
            ILoggerFactory loggerFactory,
            IEmailEncryptor emailEncryptor,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            ticker = ticker.Trim().ToUpperInvariant();
            var logger = loggerFactory.CreateLogger("OpsCatalogEndpoints");
            var fibra = await repo.GetByTickerAsync(ticker, ct);
            if (fibra is null)
            {
                return Results.NotFound();
            }

            if (fibra.State != FibraState.Inactive)
            {
                fibra.State = FibraState.Inactive;
                await repo.UpdateAsync(fibra, ct);
                logger.LogInformation(
                    "Ops {Action} FIBRA {Ticker} by {Actor} at {Timestamp}",
                    "DEACTIVATE",
                    fibra.Ticker,
                    GetActor(ctx, emailEncryptor, logger),
                    DateTimeOffset.UtcNow);
            }

            return Results.Ok(ToDto(fibra));
        })
        .Produces<FibraDetail>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/{ticker}/activate", async (
            string ticker,
            IFibraRepository repo,
            ILoggerFactory loggerFactory,
            IEmailEncryptor emailEncryptor,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            ticker = ticker.Trim().ToUpperInvariant();
            var logger = loggerFactory.CreateLogger("OpsCatalogEndpoints");
            var fibra = await repo.GetByTickerAsync(ticker, ct);
            if (fibra is null)
            {
                return Results.NotFound();
            }

            if (fibra.State != FibraState.Active)
            {
                fibra.State = FibraState.Active;
                await repo.UpdateAsync(fibra, ct);
                logger.LogInformation(
                    "Ops {Action} FIBRA {Ticker} by {Actor} at {Timestamp}",
                    "ACTIVATE",
                    fibra.Ticker,
                    GetActor(ctx, emailEncryptor, logger),
                    DateTimeOffset.UtcNow);
            }

            return Results.Ok(ToDto(fibra));
        })
        .Produces<FibraDetail>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }

    private static Dictionary<string, string[]> ValidateCreateRequest(CreateFibraRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        AddRequired(errors, "ticker", request.Ticker, 20);
        AddRequired(errors, "yahooTicker", request.YahooTicker, 32);
        AddRequired(errors, "fullName", request.FullName, 256);
        AddRequired(errors, "shortName", request.ShortName, 64);
        AddRequired(errors, "sector", request.Sector, 64);
        AddRequired(errors, "market", request.Market, 32);
        AddRequired(errors, "currency", request.Currency, 8);
        AddOptionalUrl(errors, "siteUrl", request.SiteUrl);
        AddOptionalUrl(errors, "investorUrl", request.InvestorUrl);
        AddOptionalUrl(errors, "reportsUrl", request.ReportsUrl);
        AddVariants(errors, request.NameVariants);

        if (!errors.ContainsKey("currency") &&
            !string.IsNullOrWhiteSpace(request.Currency) &&
            !AllowedCurrencies.Contains(request.Currency.Trim().ToUpperInvariant()))
        {
            errors["currency"] = ["Moneda no reconocida. Valores válidos: MXN, USD, EUR, UDI."];
        }

        if (request.Description is not null && request.Description.Length > 10_000)
        {
            errors["description"] = ["La descripción no puede superar 10 000 caracteres."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateUpdateRequest(UpdateFibraRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        AddRequired(errors, "yahooTicker", request.YahooTicker, 32);
        AddRequired(errors, "fullName", request.FullName, 256);
        AddRequired(errors, "shortName", request.ShortName, 64);
        AddRequired(errors, "sector", request.Sector, 64);
        AddRequired(errors, "market", request.Market, 32);
        AddRequired(errors, "currency", request.Currency, 8);
        AddOptionalUrl(errors, "siteUrl", request.SiteUrl);
        AddOptionalUrl(errors, "investorUrl", request.InvestorUrl);
        AddOptionalUrl(errors, "reportsUrl", request.ReportsUrl);
        AddVariants(errors, request.NameVariants);

        if (!errors.ContainsKey("currency") &&
            !string.IsNullOrWhiteSpace(request.Currency) &&
            !AllowedCurrencies.Contains(request.Currency.Trim().ToUpperInvariant()))
        {
            errors["currency"] = ["Moneda no reconocida. Valores válidos: MXN, USD, EUR, UDI."];
        }

        if (request.Description is not null && request.Description.Length > 10_000)
        {
            errors["description"] = ["La descripción no puede superar 10 000 caracteres."];
        }

        return errors;
    }

    private static void AddRequired(Dictionary<string, string[]> errors, string field, string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = [$"El campo {field} es requerido."];
            return;
        }

        if (value.Trim().Length > maxLength)
        {
            errors[field] = [$"El campo {field} no puede superar {maxLength} caracteres."];
        }
    }

    private static void AddOptionalUrl(Dictionary<string, string[]> errors, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (value.Trim().Length > 512)
        {
            errors[field] = [$"El campo {field} no puede superar 512 caracteres."];
            return;
        }

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
        {
            errors[field] = [$"El campo {field} debe ser una URL absoluta válida."];
            return;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            errors[field] = [$"El campo {field} debe usar http o https."];
        }
    }

    private static void AddVariants(Dictionary<string, string[]> errors, IReadOnlyList<string>? variants)
    {
        if (variants is null)
        {
            return;
        }

        if (variants.Any(v => string.IsNullOrWhiteSpace(v)))
        {
            errors["nameVariants"] = ["Las variantes de nombre no pueden estar vacías."];
            return;
        }

        if (variants.Any(v => v.Trim().Length > 128))
        {
            errors["nameVariants"] = ["Cada variante de nombre debe tener máximo 128 caracteres."];
        }
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static List<string> NormalizeVariants(IReadOnlyList<string>? variants)
        => variants?
            .Select(v => v.Trim())
            .Where(v => v.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

    private static FibraDetail ToDto(Fibra fibra) => new(
        fibra.Id,
        fibra.Ticker,
        fibra.YahooTicker,
        fibra.FullName,
        fibra.ShortName,
        fibra.Sector,
        fibra.Market,
        fibra.Currency,
        fibra.State.ToString(),
        fibra.SiteUrl,
        fibra.InvestorUrl,
        fibra.ReportsUrl,
        fibra.NameVariants.AsReadOnly(),
        fibra.CreatedAt,
        fibra.Description);

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
