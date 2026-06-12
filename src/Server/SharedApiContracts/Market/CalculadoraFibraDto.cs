namespace SharedApiContracts.Market;

public record CalculadoraFibraDto(
    string Ticker,
    string Empresa,
    decimal? PrecioActual,
    string? UltimoPeriodo,
    decimal? DistCbfi,
    decimal? DistCbfiAnual,
    string? FreshnessStatus
);
