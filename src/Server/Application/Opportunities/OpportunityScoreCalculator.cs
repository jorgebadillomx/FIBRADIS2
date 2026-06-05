using Domain.Catalog;
using Domain.Fundamentals;
using Domain.Market;

namespace Application.Opportunities;

public sealed record OpportunityFibraScore(
    Guid FibraId,
    string Ticker,
    string Nombre,
    decimal? Score,
    int ComponentCount,
    bool IsLimitedData,
    bool IsExcluded,
    decimal? NavDiscountScore,
    decimal? DividendYieldScore,
    decimal? LtvInvertedScore,
    decimal? NoiMarginScore,
    decimal? Pricevs52wScore,
    decimal? NavDiscountPct,
    decimal? DividendYieldPct,
    decimal? LtvPct,
    decimal? NoiMarginPct,
    decimal? PriceVsAvg52wPct,
    decimal? PrecioActual,
    decimal? NavPerCbfi,
    decimal? Avg52w);

public static class OpportunityScoreCalculator
{
    private const int MinComponentsForRanking = 3;

    public static IReadOnlyList<OpportunityFibraScore> Calculate(
        IReadOnlyList<Fibra> activeFibras,
        IReadOnlyDictionary<Guid, PriceSnapshot> snapshotByFibra,
        IReadOnlyDictionary<Guid, FundamentalRecord> fundamentalByFibra,
        IReadOnlyDictionary<Guid, decimal> annualDistByFibra,
        IReadOnlyDictionary<Guid, decimal> avg52wByFibra,
        OpportunityWeights weights)
    {
        // Step 1: Compute raw values per FIBRA
        var rawItems = activeFibras
            .Select(f => ComputeRaw(f, snapshotByFibra, fundamentalByFibra, annualDistByFibra, avg52wByFibra))
            .ToList();

        // Step 2: Percentile-normalize each component across all eligible FIBRAs (with price)
        var eligibleItems = rawItems.Where(r => r.HasPrice).ToList();

        var navDiscountPercentiles = PercentileNormalize(eligibleItems, r => r.NavDiscountRaw);
        var dividendYieldPercentiles = PercentileNormalize(eligibleItems, r => r.DividendYieldRaw);
        var ltvInvertedPercentiles = PercentileNormalize(eligibleItems, r => r.LtvInvertedRaw);
        var noiMarginPercentiles = PercentileNormalize(eligibleItems, r => r.NoiMarginRaw);
        var pricevs52wPercentiles = PercentileNormalize(eligibleItems, r => r.Pricevs52wRaw);

        // Step 3: Build final scores
        var results = new List<OpportunityFibraScore>(rawItems.Count);
        for (var i = 0; i < eligibleItems.Count; i++)
        {
            var raw = eligibleItems[i];
            var navScore = navDiscountPercentiles[i];
            var yieldScore = dividendYieldPercentiles[i];
            var ltvScore = ltvInvertedPercentiles[i];
            var noiScore = noiMarginPercentiles[i];
            var pvs52Score = pricevs52wPercentiles[i];

            var componentCount = CountNonNull(navScore, yieldScore, ltvScore, noiScore, pvs52Score);
            var isExcluded = componentCount == 0;
            var isLimitedData = !isExcluded && componentCount < MinComponentsForRanking;

            decimal? totalScore = null;
            if (!isExcluded)
            {
                var score = 0m;
                if (navScore.HasValue) score += navScore.Value * weights.NavDiscount;
                if (yieldScore.HasValue) score += yieldScore.Value * weights.DividendYield;
                if (ltvScore.HasValue) score += ltvScore.Value * weights.LtvInverted;
                if (noiScore.HasValue) score += noiScore.Value * weights.NoiMargin;
                if (pvs52Score.HasValue) score += pvs52Score.Value * weights.Pricevs52w;
                totalScore = Math.Round(score / 100m, 2);
            }

            results.Add(new OpportunityFibraScore(
                raw.FibraId, raw.Ticker, raw.Nombre,
                totalScore, componentCount, isLimitedData, isExcluded,
                navScore, yieldScore, ltvScore, noiScore, pvs52Score,
                raw.NavDiscountRaw, raw.DividendYieldRaw, raw.LtvPct, raw.NoiMarginRaw, raw.Pricevs52wRaw,
                raw.Price, raw.NavPerCbfi, raw.Avg52w));
        }

        // Add fully excluded fibras (no price)
        foreach (var raw in rawItems.Where(r => !r.HasPrice))
        {
            results.Add(new OpportunityFibraScore(
                raw.FibraId, raw.Ticker, raw.Nombre,
                null, 0, false, true,
                null, null, null, null, null,
                null, null, null, null, null,
                null, null, null));
        }

        return results;
    }

    // ── Percentile normalization ──────────────────────────────────────────────

    private static decimal?[] PercentileNormalize(
        IReadOnlyList<RawFibraData> items,
        Func<RawFibraData, decimal?> selector)
    {
        var result = new decimal?[items.Count];

        // Collect (index, value) pairs for non-null values
        var present = items
            .Select((r, i) => (i, v: selector(r)))
            .Where(x => x.v.HasValue)
            .OrderBy(x => x.v!.Value)
            .ToList();

        if (present.Count == 0)
            return result;

        if (present.Count == 1)
        {
            result[present[0].i] = 50m;
            return result;
        }

        var max = present.Count - 1;
        for (var rank = 0; rank < present.Count; rank++)
        {
            result[present[rank].i] = Math.Round((decimal)rank / max * 100m, 4);
        }

        return result;
    }

    // ── Raw value computation ─────────────────────────────────────────────────

    private static RawFibraData ComputeRaw(
        Fibra fibra,
        IReadOnlyDictionary<Guid, PriceSnapshot> snapshotByFibra,
        IReadOnlyDictionary<Guid, FundamentalRecord> fundamentalByFibra,
        IReadOnlyDictionary<Guid, decimal> annualDistByFibra,
        IReadOnlyDictionary<Guid, decimal> avg52wByFibra)
    {
        snapshotByFibra.TryGetValue(fibra.Id, out var snapshot);
        fundamentalByFibra.TryGetValue(fibra.Id, out var fund);
        annualDistByFibra.TryGetValue(fibra.Id, out var annualDist);
        avg52wByFibra.TryGetValue(fibra.Id, out var avg52w);

        var price = snapshot?.LastPrice;
        var hasPrice = price is > 0m;

        decimal? navDiscountRaw = null;
        decimal? navPerCbfi = null;
        if (hasPrice && fund?.NavPerCbfi is > 0m)
        {
            navPerCbfi = fund.NavPerCbfi;
            navDiscountRaw = Math.Round((1m - price!.Value / navPerCbfi.Value) * 100m, 4);
        }

        decimal? dividendYieldRaw = null;
        if (hasPrice && annualDist > 0m)
        {
            dividendYieldRaw = Math.Round(annualDist / price!.Value * 100m, 4);
        }

        decimal? ltvInvertedRaw = null;
        decimal? ltvPct = null;
        if (fund?.Ltv is >= 0m and <= 1m)
        {
            ltvPct = Math.Round(fund.Ltv.Value * 100m, 4);
            ltvInvertedRaw = Math.Round((1m - fund.Ltv.Value) * 100m, 4);
        }

        decimal? noiMarginRaw = null;
        if (fund?.NoiMargin is > 0m)
        {
            noiMarginRaw = Math.Round(fund.NoiMargin.Value * 100m, 4);
        }

        decimal? pricevs52wRaw = null;
        decimal? avg52wVal = avg52w > 0m ? avg52w : null;
        if (hasPrice && avg52wVal.HasValue)
        {
            pricevs52wRaw = Math.Round((1m - price!.Value / avg52wVal.Value) * 100m, 4);
        }

        var nombre = fibra.ShortName;
        if (string.IsNullOrWhiteSpace(nombre)) nombre = fibra.FullName;
        if (string.IsNullOrWhiteSpace(nombre)) nombre = fibra.Ticker;

        return new RawFibraData(
            fibra.Id, fibra.Ticker, nombre, price,
            hasPrice, navPerCbfi, avg52wVal,
            navDiscountRaw, dividendYieldRaw, ltvInvertedRaw, ltvPct, noiMarginRaw, pricevs52wRaw);
    }

    private static int CountNonNull(params decimal?[] values) =>
        values.Count(v => v.HasValue);

    private sealed record RawFibraData(
        Guid FibraId,
        string Ticker,
        string Nombre,
        decimal? Price,
        bool HasPrice,
        decimal? NavPerCbfi,
        decimal? Avg52w,
        decimal? NavDiscountRaw,
        decimal? DividendYieldRaw,
        decimal? LtvInvertedRaw,
        decimal? LtvPct,
        decimal? NoiMarginRaw,
        decimal? Pricevs52wRaw);
}
