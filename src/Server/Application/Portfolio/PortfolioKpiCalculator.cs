using Domain.Catalog;
using Domain.Market;
using System.Collections.Generic;

namespace Application.Portfolio;

public static class PortfolioKpiCalculator
{
    public static PortfolioKpiResult Calculate(
        IReadOnlyList<Domain.Portfolio.PortfolioPosition> positions,
        IReadOnlyDictionary<Guid, PriceSnapshot> snapshotByFibra,
        IReadOnlyDictionary<Guid, IReadOnlyList<Distribution>> distsByFibra,
        IReadOnlyDictionary<Guid, Fibra> fibraById)
    {
        if (positions.Count == 0)
        {
            return new PortfolioKpiResult(
                InversionTotal: 0m,
                ValorTotal: null,
                PlusvaliaTotal_Pct: null,
                PlusvaliaTotal_Mxn: null,
                YieldPortafolio: null,
                IngresoMensual: null,
                RentasAnualesBrutas: 0m,
                RentasRealesBrutas: 0m,
                PctRentasPortafolio: 0m,
                IsPartial: false,
                Positions: []);
        }

        var portfolioBase = positions.Sum(p => p.Titulos * p.CostoPromedio);
        var isPartial = false;

        var rows = positions.Select(position =>
        {
            snapshotByFibra.TryGetValue(position.FibraId, out var snapshot);
            distsByFibra.TryGetValue(position.FibraId, out var distributions);
            fibraById.TryGetValue(position.FibraId, out var fibra);

            var precioActual = snapshot?.LastPrice;
            if (precioActual is null || precioActual <= 0m)
                isPartial = true;

            decimal? valorMercado = null;
            decimal? plusvaliaFilaMxn = null;
            decimal? plusvaliaFilaPct = null;
            if (precioActual is > 0m)
            {
                valorMercado = position.Titulos * precioActual.Value;
                plusvaliaFilaMxn = valorMercado.Value - position.CostoTotalCompra;
                plusvaliaFilaPct = position.CostoTotalCompra == 0m
                    ? null
                    : Math.Round(plusvaliaFilaMxn.Value / position.CostoTotalCompra * 100m, 6);
            }

            decimal? rentaAnual = null;
            if (distributions is { Count: > 0 })
            {
                var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-365);
                var totalPerUnit = distributions
                    .Where(d => d.PaymentDate >= cutoff)
                    .Sum(d => d.AmountPerUnit);
                if (totalPerUnit > 0m)
                    rentaAnual = position.Titulos * totalPerUnit;
            }

            decimal? yoc = null;
            if (rentaAnual.HasValue && position.CostoTotalCompra > 0m)
            {
                yoc = Math.Round(rentaAnual.Value / position.CostoTotalCompra * 100m, 6);
            }

            var ticker = fibra?.Ticker ?? snapshot?.Ticker ?? position.FibraId.ToString();
            var nombre = fibra?.ShortName;
            if (string.IsNullOrWhiteSpace(nombre))
                nombre = fibra?.FullName;
            if (string.IsNullOrWhiteSpace(nombre))
                nombre = ticker;

            return new PortfolioPositionRow(
                FibraId: position.FibraId,
                Ticker: ticker,
                Nombre: nombre,
                Titulos: position.Titulos,
                CostoPromedio: position.CostoPromedio,
                CostoTotalCompra: position.CostoTotalCompra,
                PctPortafolio: portfolioBase == 0m
                    ? 0m
                    : Math.Round((position.Titulos * position.CostoPromedio) / portfolioBase * 100m, 6),
                PrecioActual: precioActual,
                ValorMercado: valorMercado,
                PlusvaliaFilaPct: plusvaliaFilaPct,
                PlusvaliaFilaMxn: plusvaliaFilaMxn,
                RentaAnual: rentaAnual,
                Yoc: yoc);
        }).ToList();

        var inversionTotal = positions.Sum(p => p.CostoTotalCompra);
        var valorTotal = rows.Where(r => r.ValorMercado.HasValue).Sum(r => r.ValorMercado!.Value);
        var hasAnyPrice = rows.Any(r => r.ValorMercado.HasValue);
        var hasMissingPrice = rows.Any(r => !r.ValorMercado.HasValue);

        decimal? plusvaliaTotalMxn = null;
        decimal? plusvaliaTotalPct = null;
        if (hasAnyPrice && !hasMissingPrice)
        {
            plusvaliaTotalMxn = valorTotal - inversionTotal;
            plusvaliaTotalPct = inversionTotal == 0m
                ? null
                : Math.Round(plusvaliaTotalMxn.Value / inversionTotal * 100m, 6);
        }

        var rentasAnualesBrutas = rows.Where(r => r.RentaAnual.HasValue).Sum(r => r.RentaAnual!.Value);
        var rentasRealesBrutas = rentasAnualesBrutas;
        decimal? yieldPortafolio = hasAnyPrice && !hasMissingPrice && valorTotal > 0m
            ? Math.Round(rentasAnualesBrutas / valorTotal * 100m, 6)
            : null;
        var ingresoMensual = rentasAnualesBrutas == 0m
            ? 0m
            : Math.Round(rentasAnualesBrutas / 12m, 2);

        return new PortfolioKpiResult(
            InversionTotal: inversionTotal,
            ValorTotal: hasAnyPrice ? valorTotal : null,
            PlusvaliaTotal_Pct: plusvaliaTotalPct,
            PlusvaliaTotal_Mxn: plusvaliaTotalMxn,
            YieldPortafolio: yieldPortafolio,
            IngresoMensual: ingresoMensual,
            RentasAnualesBrutas: rentasAnualesBrutas,
            RentasRealesBrutas: rentasRealesBrutas,
            PctRentasPortafolio: inversionTotal == 0m
                ? 0m
                : Math.Round(rentasAnualesBrutas / inversionTotal * 100m, 6),
            IsPartial: isPartial,
            Positions: rows);
    }
}
