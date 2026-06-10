namespace Application.Portfolio;

public sealed record PortfolioKpiResult(
    decimal InversionTotal,
    decimal? ValorTotal,
    decimal? PlusvaliaTotal_Pct,
    decimal? PlusvaliaTotal_Mxn,
    decimal? YieldPortafolio,
    decimal? IngresoMensual,
    decimal RentasAnualesBrutas,
    decimal RentasRealesBrutas,
    decimal PctRentasPortafolio,
    bool IsPartial,
    IReadOnlyList<PortfolioPositionRow> Positions);

public sealed record PortfolioPositionRow(
    Guid FibraId,
    string Ticker,
    string Nombre,
    int Titulos,
    decimal CostoPromedio,
    decimal CostoTotalCompra,
    decimal PctPortafolio,
    decimal? PrecioActual,
    decimal? ValorMercado,
    decimal? PlusvaliaFilaPct,
    decimal? PlusvaliaFilaMxn,
    decimal? RentaAnual,
    decimal? Yoc);
