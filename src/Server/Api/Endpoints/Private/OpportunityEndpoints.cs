using System.Security.Claims;
using System.Text.Json;
using Application.Catalog;
using Application.Fundamentals;
using Application.Market;
using Application.Opportunities;
using Application.Ops;
using Domain.Catalog;
using Domain.Fundamentals;
using Domain.Market;
using Microsoft.Extensions.Caching.Memory;
using SharedApiContracts.Opportunities;

namespace Api.Endpoints.Private;

public static class OpportunityEndpoints
{
    private const string RawDataCacheKey = "opp:raw:v1";

    private sealed record RawOpportunityData(
        IReadOnlyList<Fibra> Fibras,
        IReadOnlyDictionary<Guid, PriceSnapshot> SnapshotByFibra,
        IReadOnlyDictionary<Guid, FundamentalRecord> FundamentalByFibra,
        IReadOnlyDictionary<Guid, decimal> AnnualDistByFibra,
        IReadOnlyDictionary<Guid, decimal> Avg52wByFibra,
        int FibrasWithPrice,
        DateTimeOffset? LastValidPriceAt);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapOpportunities(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/opportunities")
            .RequireAuthorization("User")
            .WithTags("Opportunities");

        // GET /api/v1/opportunities — ranking completo con pesos del usuario
        group.MapGet("/", async (
            IOpportunityWeightsRepository weightsRepo,
            IFibraRepository fibraRepo,
            IMarketRepository marketRepo,
            IFundamentalRepository fundamentalRepo,
            IOperationalConfigRepository configRepo,
            IMemoryCache cache,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (TryGetUserId(ctx) is not { } userId)
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);

            var weights = await ResolveWeightsAsync(weightsRepo, userId, ct);
            var config = await configRepo.GetAsync(ct);

            if (!cache.TryGetValue<RawOpportunityData>(RawDataCacheKey, out var raw) || raw is null)
            {
                // DbContext es Scoped y no thread-safe — await secuencial obligatorio
                var fibras = await fibraRepo.GetAllActiveAsync(ct);
                var snapshots = await marketRepo.GetLatestSnapshotPerFibraAsync(ct);
                var fundamentals = await fundamentalRepo.GetSummaryLatestAsync(ct);
                var fibraIds = fibras.Select(f => f.Id).ToList();
                var distributions = await marketRepo.GetDistributionsByFibrasAsync(fibraIds, 365, ct);
                var avg52w = await marketRepo.GetWeek52AvgByFibrasAsync(fibraIds, 365, ct);

                var snapshotByFibra = snapshots.ToDictionary(s => s.FibraId);
                var fundamentalByFibra = fundamentals
                    .ToDictionary(f => f.Record.FibraId, f => f.Record);
                var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-365);
                var annualDistByFibra = distributions
                    .GroupBy(d => d.FibraId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Where(d => d.PaymentDate >= cutoff).Sum(d => d.AmountPerUnit));
                var fibrasWithPrice = fibras.Count(f =>
                    snapshotByFibra.TryGetValue(f.Id, out var snap) && snap.LastPrice is > 0m);
                var lastValidPriceAt = snapshotByFibra.Values
                    .Where(s => s.LastPrice is > 0m)
                    .MaxBy(s => s.CapturedAt)?.CapturedAt;

                raw = new RawOpportunityData(fibras, snapshotByFibra, fundamentalByFibra, annualDistByFibra, avg52w, fibrasWithPrice, lastValidPriceAt);
                cache.Set(RawDataCacheKey, raw, TimeSpan.FromMinutes(15));
            }

            var scores = OpportunityScoreCalculator.Calculate(
                raw.Fibras,
                raw.SnapshotByFibra,
                raw.FundamentalByFibra,
                raw.AnnualDistByFibra,
                raw.Avg52wByFibra,
                weights);

            var ranked = scores
                .Where(s => !s.IsLimitedData && !s.IsExcluded)
                .OrderByDescending(s => s.Score)
                .Select(ToRowDto)
                .ToList();

            var limitedData = scores
                .Where(s => s.IsLimitedData)
                .OrderByDescending(s => s.Score)
                .Select(ToRowDto)
                .ToList();

            var coverage = UniverseCoverageCalculator.Calculate(
                raw.Fibras.Count, raw.FibrasWithPrice,
                config.UniverseDegradationThresholdPct, raw.LastValidPriceAt);
            var coverageDto = new UniverseCoverageDto(
                coverage.UniverseSize, coverage.FibrasWithPrice, coverage.MissingPct,
                coverage.DegradationThresholdPct, coverage.Status, coverage.LastValidPriceAt);

            return Results.Ok(new OpportunityRankingResponseDto(ranked, limitedData, ToDto(weights), coverageDto));
        })
        .Produces<OpportunityRankingResponseDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        // GET /api/v1/opportunities/weights — pesos actuales del usuario
        group.MapGet("/weights", async (
            IOpportunityWeightsRepository weightsRepo,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (TryGetUserId(ctx) is not { } userId)
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);

            var weights = await ResolveWeightsAsync(weightsRepo, userId, ct);
            return Results.Ok(ToDto(weights));
        })
        .Produces<OpportunityWeightsDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        // PUT /api/v1/opportunities/weights — guarda pesos del usuario
        group.MapPut("/weights", async (
            OpportunityWeightsDto request,
            IOpportunityWeightsRepository weightsRepo,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (!ValidateWeights(request, out var problem))
                return Results.Problem(problem, statusCode: StatusCodes.Status422UnprocessableEntity);

            if (TryGetUserId(ctx) is not { } userId)
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);
            var weights = new Domain.Portfolio.UserOpportunityWeights
            {
                UserId = userId,
                WeightsJson = JsonSerializer.Serialize(new
                {
                    navDiscount = request.NavDiscount,
                    dividendYield = request.DividendYield,
                    ltvInverted = request.LtvInverted,
                    noiMargin = request.NoiMargin,
                    pricevs52w = request.Pricevs52w,
                }, JsonOptions),
                Profile = request.Profile,
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            await weightsRepo.UpsertAsync(weights, ct);
            return Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Guid? TryGetUserId(HttpContext ctx) =>
        Guid.TryParse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private static async Task<OpportunityWeights> ResolveWeightsAsync(
        IOpportunityWeightsRepository repo, Guid userId, CancellationToken ct)
    {
        var stored = await repo.GetByUserIdAsync(userId, ct);
        if (stored is null)
            return OpportunityWeights.Default;

        if (stored.Profile != "custom" && stored.Profile != null)
            return OpportunityWeights.FromProfile(stored.Profile);

        if (stored.WeightsJson is null)
            return OpportunityWeights.Default;

        try
        {
            var json = JsonSerializer.Deserialize<WeightsJson>(stored.WeightsJson, JsonOptions);
            if (json is null) return OpportunityWeights.Default;
            return new OpportunityWeights(json.NavDiscount, json.DividendYield, json.LtvInverted,
                json.NoiMargin, json.Pricevs52w, "custom");
        }
        catch (JsonException)
        {
            return OpportunityWeights.Default;
        }
    }

    private static bool ValidateWeights(OpportunityWeightsDto dto, out string problem)
    {
        var components = new[] { dto.NavDiscount, dto.DividendYield, dto.LtvInverted, dto.NoiMargin, dto.Pricevs52w };
        if (components.Any(w => w < 0m || w > 100m))
        {
            problem = "Cada peso debe estar entre 0 y 100.";
            return false;
        }
        var sum = components.Sum();
        if (Math.Abs(sum - 100m) > 0.01m)
        {
            problem = $"Los pesos deben sumar 100. Suma actual: {sum}.";
            return false;
        }
        var validProfiles = new[] { "default", "renta", "crecimiento", "custom" };
        if (!validProfiles.Contains(dto.Profile))
        {
            problem = $"Perfil inválido: '{dto.Profile}'.";
            return false;
        }
        problem = string.Empty;
        return true;
    }

    private static OpportunityWeightsDto ToDto(OpportunityWeights w) =>
        new(w.NavDiscount, w.DividendYield, w.LtvInverted, w.NoiMargin, w.Pricevs52w, w.Profile);

    private static OpportunityFibraRowDto ToRowDto(OpportunityFibraScore s) =>
        new(s.FibraId, s.Ticker, s.Nombre, s.Score, s.ComponentCount, s.IsLimitedData,
            s.NavDiscountScore, s.DividendYieldScore, s.LtvInvertedScore, s.NoiMarginScore, s.Pricevs52wScore,
            s.NavDiscountPct, s.DividendYieldPct, s.LtvPct, s.NoiMarginPct, s.PriceVsAvg52wPct,
            s.PrecioActual, s.NavPerCbfi, s.Avg52w);

    private sealed record WeightsJson(
        decimal NavDiscount, decimal DividendYield, decimal LtvInverted,
        decimal NoiMargin, decimal Pricevs52w);
}
