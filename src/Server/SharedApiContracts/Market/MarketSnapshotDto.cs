namespace SharedApiContracts.Market;

public record MarketSnapshotDto(
    Guid FibraId,
    string Ticker,
    decimal? LastPrice,
    decimal? DailyChange,
    decimal? DailyChangePct,
    long? Volume,
    decimal? Week52High,
    decimal? Week52Low,
    string? CapturedAt,       // ISO 8601 UTC, e.g. "2026-05-19T14:30:00Z", null si no hay dato
    string? FreshnessStatus   // "fresh" | "stale" | "off-hours" | "critical" | null
);
