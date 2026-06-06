using Application.Opportunities;
using Domain.Catalog;
using Domain.Fundamentals;
using Domain.Market;

namespace Application.Tests.Opportunities;

public class OpportunityScoreCalculatorTests
{
    private static readonly Guid FunoId = Guid.NewGuid();
    private static readonly Guid VfiId = Guid.NewGuid();
    private static readonly Guid TerraId = Guid.NewGuid();

    private static Fibra MakeFibra(Guid id, string ticker) => new()
    {
        Id = id,
        Ticker = ticker,
        ShortName = ticker,
        FullName = ticker,
        State = FibraState.Active,
    };

    private static PriceSnapshot MakeSnapshot(Guid fibraId, decimal price) => new()
    {
        Id = Guid.NewGuid(),
        FibraId = fibraId,
        Ticker = "X",
        LastPrice = price,
        CapturedAt = DateTimeOffset.UtcNow,
        Status = MarketDataStatus.Processed,
    };

    private static FundamentalRecord MakeFundamental(Guid fibraId,
        decimal? nav = null, decimal? ltv = null, decimal? noi = null) => new()
    {
        Id = Guid.NewGuid(),
        FibraId = fibraId,
        Period = "Q1-2026",
        Status = "processed",
        NavPerCbfi = nav,
        Ltv = ltv,
        NoiMargin = noi,
    };

    [Fact]
    public void Calculate_SingleFibra_WithAllComponents_PercentileIs50()
    {
        var fibra = MakeFibra(FunoId, "FUNO11");
        var snapshot = MakeSnapshot(FunoId, 24.00m);
        var fund = MakeFundamental(FunoId, nav: 30.00m, ltv: 0.40m, noi: 0.55m);

        var scores = OpportunityScoreCalculator.Calculate(
            [fibra],
            new Dictionary<Guid, PriceSnapshot> { [FunoId] = snapshot },
            new Dictionary<Guid, FundamentalRecord> { [FunoId] = fund },
            new Dictionary<Guid, decimal> { [FunoId] = 1.50m },   // annual dist
            new Dictionary<Guid, decimal> { [FunoId] = 26.00m },  // avg52w
            OpportunityWeights.Default);

        var result = scores.Single(s => s.FibraId == FunoId);
        Assert.False(result.IsExcluded);
        Assert.False(result.IsLimitedData);
        Assert.Equal(5, result.ComponentCount);
        // Con una sola fibra, todos los percentiles son 50 → score = 50
        Assert.Equal(50m, result.Score!.Value);
    }

    [Fact]
    public void Calculate_FibraWithoutPrice_IsExcluded()
    {
        var fibra = MakeFibra(FunoId, "FUNO11");
        var fund = MakeFundamental(FunoId, nav: 30.00m, ltv: 0.40m, noi: 0.55m);

        var scores = OpportunityScoreCalculator.Calculate(
            [fibra],
            new Dictionary<Guid, PriceSnapshot>(),  // sin precio
            new Dictionary<Guid, FundamentalRecord> { [FunoId] = fund },
            new Dictionary<Guid, decimal>(),
            new Dictionary<Guid, decimal>(),
            OpportunityWeights.Default);

        var result = scores.Single(s => s.FibraId == FunoId);
        Assert.True(result.IsExcluded);
        Assert.Null(result.Score);
    }

    [Fact]
    public void Calculate_FibraWith2Components_IsLimitedData()
    {
        var fibra = MakeFibra(FunoId, "FUNO11");
        var snapshot = MakeSnapshot(FunoId, 24.00m);
        // Solo NAV y Yield (2 componentes)
        var fund = MakeFundamental(FunoId, nav: 30.00m);

        var scores = OpportunityScoreCalculator.Calculate(
            [fibra],
            new Dictionary<Guid, PriceSnapshot> { [FunoId] = snapshot },
            new Dictionary<Guid, FundamentalRecord> { [FunoId] = fund },
            new Dictionary<Guid, decimal> { [FunoId] = 1.50m },
            new Dictionary<Guid, decimal>(),
            OpportunityWeights.Default);

        var result = scores.Single(s => s.FibraId == FunoId);
        Assert.True(result.IsLimitedData);
        Assert.Equal(2, result.ComponentCount);
        Assert.False(result.IsExcluded);
    }

    [Fact]
    public void Calculate_TwoFibras_BetterNavDiscountRanksHigher()
    {
        // FUNO: precio = 20, NAV = 30 → descuento 33% (mejor)
        // VFI: precio = 28, NAV = 30 → descuento 6.7% (peor)
        // Sin distribuciones, sin LTV, sin NOI, sin 52S → solo Descuento NAV disponible
        var fibraFuno = MakeFibra(FunoId, "FUNO11");
        var fibraVfi = MakeFibra(VfiId, "VFIN");

        var snapshots = new Dictionary<Guid, PriceSnapshot>
        {
            [FunoId] = MakeSnapshot(FunoId, 20.00m),
            [VfiId] = MakeSnapshot(VfiId, 28.00m),
        };
        var funds = new Dictionary<Guid, FundamentalRecord>
        {
            [FunoId] = MakeFundamental(FunoId, nav: 30.00m),
            [VfiId] = MakeFundamental(VfiId, nav: 30.00m),
        };

        var scores = OpportunityScoreCalculator.Calculate(
            [fibraFuno, fibraVfi],
            snapshots, funds,
            new Dictionary<Guid, decimal>(),
            new Dictionary<Guid, decimal>(),
            OpportunityWeights.Default);

        var funoScore = scores.Single(s => s.FibraId == FunoId);
        var vfiScore = scores.Single(s => s.FibraId == VfiId);

        // FUNO tiene mayor descuento → percentil más alto → score mayor
        Assert.True(funoScore.Score > vfiScore.Score,
            $"FUNO ({funoScore.Score}) should rank higher than VFI ({vfiScore.Score})");
    }

    [Fact]
    public void Calculate_WithRentaProfile_AppliesCorrectWeights()
    {
        // Con perfil Renta: Yield 50%, NAV 20%, LTV 10%, NOI 20%, 52S 0%
        // FUNO: alto yield, bajo descuento NAV
        // VFI: bajo yield, alto descuento NAV
        // Renta debería preferir FUNO (mayor yield)
        var fibraFuno = MakeFibra(FunoId, "FUNO11");
        var fibraVfi = MakeFibra(VfiId, "VFIN");

        var snapshots = new Dictionary<Guid, PriceSnapshot>
        {
            [FunoId] = MakeSnapshot(FunoId, 25.00m),
            [VfiId] = MakeSnapshot(VfiId, 25.00m),
        };
        var funds = new Dictionary<Guid, FundamentalRecord>
        {
            [FunoId] = MakeFundamental(FunoId, nav: 26.00m),  // pequeño descuento
            [VfiId] = MakeFundamental(VfiId, nav: 50.00m),    // gran descuento
        };
        // FUNO: distribución anual alta; VFI: distribución baja
        var annualDist = new Dictionary<Guid, decimal>
        {
            [FunoId] = 5.00m,   // yield = 20%
            [VfiId] = 0.50m,    // yield = 2%
        };

        var scores = OpportunityScoreCalculator.Calculate(
            [fibraFuno, fibraVfi],
            snapshots, funds, annualDist,
            new Dictionary<Guid, decimal>(),
            OpportunityWeights.Renta);

        var funoScore = scores.Single(s => s.FibraId == FunoId);
        var vfiScore = scores.Single(s => s.FibraId == VfiId);

        Assert.True(funoScore.Score > vfiScore.Score,
            $"Con perfil Renta, FUNO (alto yield) debería superar a VFI ({funoScore.Score} vs {vfiScore.Score})");
    }

    [Fact]
    public void Calculate_PercentileNormalization_MinAndMaxAreCorrect()
    {
        // 3 fibras con descuentos: 0%, 10%, 20%
        // Percentiles esperados: 0, 50, 100
        var fibras = new List<Fibra>
        {
            MakeFibra(FunoId, "FUNO11"),
            MakeFibra(VfiId, "VFIN"),
            MakeFibra(TerraId, "TERRA13"),
        };
        var snapshots = new Dictionary<Guid, PriceSnapshot>
        {
            [FunoId] = MakeSnapshot(FunoId, 30.00m),   // 0% descuento vs NAV 30
            [VfiId] = MakeSnapshot(VfiId, 27.00m),     // 10% descuento vs NAV 30
            [TerraId] = MakeSnapshot(TerraId, 24.00m), // 20% descuento vs NAV 30
        };
        var funds = new Dictionary<Guid, FundamentalRecord>
        {
            [FunoId] = MakeFundamental(FunoId, nav: 30.00m),
            [VfiId] = MakeFundamental(VfiId, nav: 30.00m),
            [TerraId] = MakeFundamental(TerraId, nav: 30.00m),
        };

        var scores = OpportunityScoreCalculator.Calculate(
            fibras,
            snapshots, funds,
            new Dictionary<Guid, decimal>(),
            new Dictionary<Guid, decimal>(),
            OpportunityWeights.Default);

        // Solo navDiscount presente → score = navDiscountPercentil × 30 / 100
        var funoScore = scores.Single(s => s.FibraId == FunoId);
        var vfiScore = scores.Single(s => s.FibraId == VfiId);
        var terraScore = scores.Single(s => s.FibraId == TerraId);

        // TERRA tiene mayor descuento → percentil 100 → score = 100 × 30 / 100 = 30
        Assert.Equal(30.00m, terraScore.Score!.Value, precision: 1);
        // FUNO tiene 0% descuento → percentil 0 → score = 0
        Assert.Equal(0.00m, funoScore.Score!.Value, precision: 1);
        // VFI en el medio → percentil 50 → score = 50 × 30 / 100 = 15
        Assert.Equal(15.00m, vfiScore.Score!.Value, precision: 1);
    }

    [Fact]
    public void Calculate_NavDiscountNegative_FlooredToZero()
    {
        // Precio > NAV → descuento negativo sin floor daría un valor < 0
        var fibra = MakeFibra(FunoId, "FUNO11");
        var snapshot = MakeSnapshot(FunoId, 35.00m); // precio 35 > NAV 30
        var fund = MakeFundamental(FunoId, nav: 30.00m);

        var scores = OpportunityScoreCalculator.Calculate(
            [fibra],
            new Dictionary<Guid, PriceSnapshot> { [FunoId] = snapshot },
            new Dictionary<Guid, FundamentalRecord> { [FunoId] = fund },
            new Dictionary<Guid, decimal>(),
            new Dictionary<Guid, decimal>(),
            OpportunityWeights.Default);

        var score = scores.Single(s => s.FibraId == FunoId);
        Assert.NotNull(score.NavDiscountScore);      // componente disponible (no null)
        Assert.Equal(0m, score.NavDiscountPct!.Value); // valor bruto flooreado a 0
    }

    [Fact]
    public void Calculate_Pricevs52wNegative_FlooredToZero()
    {
        // Precio > avg52w → valor negativo sin floor
        var fibra = MakeFibra(FunoId, "FUNO11");
        var snapshot = MakeSnapshot(FunoId, 12.00m); // precio 12 > avg52w 10
        var avg52w = new Dictionary<Guid, decimal> { [FunoId] = 10.00m };

        var scores = OpportunityScoreCalculator.Calculate(
            [fibra],
            new Dictionary<Guid, PriceSnapshot> { [FunoId] = snapshot },
            new Dictionary<Guid, FundamentalRecord>(),
            new Dictionary<Guid, decimal>(),
            avg52w,
            OpportunityWeights.Default);

        var score = scores.Single(s => s.FibraId == FunoId);
        Assert.NotNull(score.Pricevs52wScore);             // componente disponible
        Assert.Equal(0m, score.PriceVsAvg52wPct!.Value);   // valor bruto flooreado a 0
    }
}
