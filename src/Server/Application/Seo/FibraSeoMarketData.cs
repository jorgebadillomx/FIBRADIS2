using Domain.Fundamentals;
using Domain.Market;

namespace Application.Seo;

public sealed record FibraSeoMarketData(
    PriceSnapshot? LatestSnapshot,
    IReadOnlyList<Distribution> Distributions,
    decimal? QuarterlyDistribution,
    DateOnly? AsOfDate = null);
