namespace SharedApiContracts.Market;

public sealed record IndicadoresDto(
    decimal? Cetes28d,
    decimal? Tiie28d,
    DateTimeOffset? LastUpdated,
    IReadOnlyList<InpcMonthlyDto>? InpcHistory);
