using Application.Portfolio;
using Domain.Catalog;
using Domain.Market;

namespace Infrastructure.Tests.Portfolio;

public class PortfolioKpiCalculatorTests
{
    private static readonly Guid FibraUnoId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid FibraDosId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid FibraTresId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static Fibra MakeFibra(Guid id, string ticker, string shortName) => new()
    {
        Id = id,
        Ticker = ticker,
        YahooTicker = ticker,
        FullName = $"{shortName} S.A.B. de C.V.",
        ShortName = shortName,
        Sector = "FIBRA",
        Market = "BMV",
        Currency = "MXN",
        State = FibraState.Active,
    };

    private static Domain.Portfolio.PortfolioPosition MakePosition(Guid fibraId, int titulos, decimal costoPromedio, decimal costoTotalCompra) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        FibraId = fibraId,
        Titulos = titulos,
        CostoPromedio = costoPromedio,
        CostoTotalCompra = costoTotalCompra,
        UploadedAt = DateTimeOffset.UtcNow,
    };

    private static PriceSnapshot MakeSnapshot(Guid fibraId, string ticker, decimal? lastPrice, decimal? dailyChangePct = null, decimal? week52High = null)
        => new()
        {
            FibraId = fibraId,
            Ticker = ticker,
            LastPrice = lastPrice,
            DailyChangePct = dailyChangePct,
            Week52High = week52High,
            CapturedAt = DateTimeOffset.UtcNow,
            Status = MarketDataStatus.Processed,
        };

    private static Distribution MakeDistribution(Guid fibraId, string ticker, int daysAgo, decimal amountPerUnit) => new()
    {
        FibraId = fibraId,
        Ticker = ticker,
        PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-daysAgo),
        AmountPerUnit = amountPerUnit,
        CapturedAt = DateTimeOffset.UtcNow,
    };

    private static IReadOnlyDictionary<Guid, PriceSnapshot> SnapshotMap(params PriceSnapshot[] snapshots)
        => snapshots.ToDictionary(s => s.FibraId);

    private static IReadOnlyDictionary<Guid, IReadOnlyList<Distribution>> DistributionMap(params Distribution[] distributions)
        => distributions
            .GroupBy(d => d.FibraId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Distribution>)g.ToList());

    private static IReadOnlyDictionary<Guid, Fibra> FibraMap(params Fibra[] fibras)
        => fibras.ToDictionary(f => f.Id);

    [Fact]
    public void Calculate_AllPositionsWithPrice_ReturnsCorrectKpis()
    {
        var posiciones = new[]
        {
            MakePosition(FibraUnoId, 800, 46.25m, 37278.25m),
            MakePosition(FibraDosId, 400, 24.10m, 9650.00m),
        };
        var snapshots = SnapshotMap(
            MakeSnapshot(FibraUnoId, "FUNO11", 50.00m),
            MakeSnapshot(FibraDosId, "FIHO12", 26.00m));
        var distributions = DistributionMap(
            MakeDistribution(FibraUnoId, "FUNO11", 30, 1.00m),
            MakeDistribution(FibraUnoId, "FUNO11", 120, 1.00m),
            MakeDistribution(FibraDosId, "FIHO12", 60, 0.50m),
            MakeDistribution(FibraDosId, "FIHO12", 240, 0.50m));
        var fibras = FibraMap(
            MakeFibra(FibraUnoId, "FUNO11", "Fibra Uno"),
            MakeFibra(FibraDosId, "FIHO12", "Fibra Hotel"));

        var result = PortfolioKpiCalculator.Calculate(posiciones, snapshots, distributions, fibras);

        Assert.False(result.IsPartial);
        Assert.Equal(46928.25m, result.InversionTotal);
        Assert.Equal(50400m, result.ValorTotal);
        Assert.Equal(3471.75m, result.PlusvaliaTotal_Mxn);
        Assert.Equal(7.397996m, result.PlusvaliaTotal_Pct);
        Assert.Equal(2000m, result.RentasAnualesBrutas);
        Assert.Equal(2000m, result.RentasRealesBrutas);
        Assert.Equal(4.261825m, result.PctRentasPortafolio);
        Assert.Equal(2, result.Positions.Count);
    }

    [Fact]
    public void Calculate_OnePositionMissingPrice_IsPartialTrue()
    {
        var posiciones = new[]
        {
            MakePosition(FibraUnoId, 800, 46.25m, 37278.25m),
            MakePosition(FibraDosId, 400, 24.10m, 9650.00m),
        };
        var snapshots = SnapshotMap(
            MakeSnapshot(FibraUnoId, "FUNO11", 50.00m));
        var distributions = DistributionMap();
        var fibras = FibraMap(
            MakeFibra(FibraUnoId, "FUNO11", "Fibra Uno"),
            MakeFibra(FibraDosId, "FIHO12", "Fibra Hotel"));

        var result = PortfolioKpiCalculator.Calculate(posiciones, snapshots, distributions, fibras);

        Assert.True(result.IsPartial);
        Assert.Equal(46928.25m, result.InversionTotal);
        Assert.Equal(40000m, result.ValorTotal);
        Assert.Null(result.PlusvaliaTotal_Mxn);
        Assert.Null(result.PlusvaliaTotal_Pct);
        Assert.Equal(2, result.Positions.Count);
        var missingRow = result.Positions.Single(p => p.FibraId == FibraDosId);
        Assert.Null(missingRow.PrecioActual);
        Assert.Null(missingRow.ValorMercado);
        Assert.Null(missingRow.PlusvaliaFilaMxn);
        Assert.Null(missingRow.PlusvaliaFilaPct);
    }

    [Fact]
    public void Calculate_WithDistributions_ReturnsCorrectRentas()
    {
        var posiciones = new[]
        {
            MakePosition(FibraUnoId, 800, 46.25m, 37278.25m),
        };
        var snapshots = SnapshotMap(
            MakeSnapshot(FibraUnoId, "FUNO11", 50.00m));
        var distributions = DistributionMap(
            MakeDistribution(FibraUnoId, "FUNO11", 20, 1.00m),
            MakeDistribution(FibraUnoId, "FUNO11", 110, 1.50m),
            MakeDistribution(FibraUnoId, "FUNO11", 200, 2.00m));
        var fibras = FibraMap(MakeFibra(FibraUnoId, "FUNO11", "Fibra Uno"));

        var result = PortfolioKpiCalculator.Calculate(posiciones, snapshots, distributions, fibras);

        Assert.Equal(3600m, result.RentasAnualesBrutas);
        Assert.Equal(3600m, result.RentasRealesBrutas);
        Assert.Equal(3600m, result.Positions[0].RentaAnual);
    }

    [Fact]
    public void Calculate_NoPriceAnyPosition_ValorTotalNull()
    {
        var posiciones = new[]
        {
            MakePosition(FibraUnoId, 800, 46.25m, 37278.25m),
            MakePosition(FibraDosId, 400, 24.10m, 9650.00m),
        };
        var snapshots = SnapshotMap();
        var distributions = DistributionMap();
        var fibras = FibraMap(
            MakeFibra(FibraUnoId, "FUNO11", "Fibra Uno"),
            MakeFibra(FibraDosId, "FIHO12", "Fibra Hotel"));

        var result = PortfolioKpiCalculator.Calculate(posiciones, snapshots, distributions, fibras);

        Assert.True(result.IsPartial);
        Assert.Null(result.ValorTotal);
        Assert.Null(result.PlusvaliaTotal_Mxn);
        Assert.Null(result.PlusvaliaTotal_Pct);
    }

    [Fact]
    public void Calculate_PctPortafolio_BasedOnCostoPromedio()
    {
        var posiciones = new[]
        {
            MakePosition(FibraUnoId, 800, 50.00m, 40000.00m),
            MakePosition(FibraDosId, 200, 25.00m, 5000.00m),
        };
        var snapshots = SnapshotMap(
            MakeSnapshot(FibraUnoId, "FUNO11", 50.00m),
            MakeSnapshot(FibraDosId, "FIHO12", 26.00m));
        var distributions = DistributionMap();
        var fibras = FibraMap(
            MakeFibra(FibraUnoId, "FUNO11", "Fibra Uno"),
            MakeFibra(FibraDosId, "FIHO12", "Fibra Hotel"));

        var result = PortfolioKpiCalculator.Calculate(posiciones, snapshots, distributions, fibras);

        Assert.Equal(88.888889m, result.Positions[0].PctPortafolio);
        Assert.Equal(11.111111m, result.Positions[1].PctPortafolio);
    }

    [Fact]
    public void Calculate_EmptyPositions_ReturnsEmptyResult()
    {
        var result = PortfolioKpiCalculator.Calculate(
            [],
            SnapshotMap(),
            DistributionMap(),
            FibraMap());

        Assert.Equal(0m, result.InversionTotal);
        Assert.Null(result.ValorTotal);
        Assert.Empty(result.Positions);
        Assert.False(result.IsPartial);
    }
}
