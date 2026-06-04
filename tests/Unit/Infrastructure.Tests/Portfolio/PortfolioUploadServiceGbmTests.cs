using ClosedXML.Excel;
using Domain.Catalog;
using Infrastructure.Portfolio;

namespace Infrastructure.Tests.Portfolio;

public class PortfolioUploadServiceGbmTests
{
    private static readonly Guid AnauId = Guid.NewGuid();
    private static readonly Guid IuesId = Guid.NewGuid();
    private static readonly Guid IuitId = Guid.NewGuid();
    private static readonly Guid FhipoId = Guid.NewGuid();
    private static readonly Guid FunoId = Guid.NewGuid();

    private readonly PortfolioUploadService _svc = new();

    private static Fibra MakeFibra(string ticker, Guid id) => new()
    {
        Id = id,
        Ticker = ticker,
        YahooTicker = ticker,
        FullName = ticker,
        ShortName = ticker,
        Sector = "FIBRA",
        Market = "BMV",
        Currency = "MXN",
        State = FibraState.Active,
    };

    private static IReadOnlyList<Fibra> MakeCatalog() =>
    [
        MakeFibra("ANAUN", AnauId),
        MakeFibra("IUESN", IuesId),
        MakeFibra("IUITN", IuitId),
        MakeFibra("FHIPO14", FhipoId),
        MakeFibra("FUNO11", FunoId),
    ];

    private static Stream MakeWorkbook(Action<IXLWorksheet> configure)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Sheet1");
        configure(ws);

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public async Task ParseAndValidate_OfficialFormat_IsDetectedByHeaderAndSkipsSectionHeaders()
    {
        var stream = MakeWorkbook(ws =>
        {
            ws.Cell(1, 1).Value = "Emisora/Fondo";
            ws.Cell(2, 1).Value = "Mercado de Capitales Global";
            ws.Cell(3, 1).Value = "Emisora/Fondo";
            ws.Cell(4, 1).Value = "ANAU N";
            ws.Cell(4, 2).Value = 10;
            ws.Cell(4, 3).Value = "$100.00";
            ws.Cell(5, 1).Value = "EFEC.";
            ws.Cell(5, 2).Value = 1;
            ws.Cell(5, 3).Value = "$0.00";
            ws.Cell(6, 1).Value = "IUES N";
            ws.Cell(6, 2).Value = 5;
            ws.Cell(6, 3).Value = "$3,586.00";
            ws.Cell(7, 1).Value = "Mercado de Capitales Nacional";
            ws.Cell(8, 1).Value = "IUIT N";
            ws.Cell(8, 2).Value = 12;
            ws.Cell(8, 3).Value = "$45.00";
        });

        var result = await _svc.ParseAndValidateAsync(stream, "gbm.xlsx", MakeCatalog(), 0m);

        Assert.True(result.Success);
        Assert.Empty(result.Errors);
        Assert.Equal(3, result.Positions.Count);
        Assert.Contains(result.Positions, p => p.Ticker == "ANAUN" && p.Titulos == 10 && p.CostoPromedio == 100m);
        Assert.Contains(result.Positions, p => p.Ticker == "IUESN" && p.CostoPromedio == 3586m);
        Assert.Contains(result.Positions, p => p.Ticker == "IUITN" && p.Titulos == 12);
    }

    [Fact]
    public async Task ParseAndValidate_OfficialFormat_NegativeCurrencyStringIsParsedAndRejected()
    {
        var stream = MakeWorkbook(ws =>
        {
            ws.Cell(1, 1).Value = "Emisora/Fondo";
            ws.Cell(2, 1).Value = "FUNO11";
            ws.Cell(2, 2).Value = 10;
            ws.Cell(2, 3).Value = "$-4.57";
        });

        var result = await _svc.ParseAndValidateAsync(stream, "gbm.xlsx", MakeCatalog(), 0m);

        Assert.False(result.Success);
        Assert.Single(result.Errors);
        Assert.Contains("AvgCost", result.Errors[0].Message);
    }

    [Fact]
    public async Task ParseAndValidate_PastedFormat_IsDetectedByHeaderAndResolvesTwoRowPattern()
    {
        var stream = MakeWorkbook(ws =>
        {
            ws.Cell(1, 1).Value = "Emisora";
            ws.Cell(2, 1).Value = "&nbsp;";
            ws.Cell(2, 2).Value = 100;
            ws.Cell(2, 3).Value = 25.5;
            ws.Cell(3, 1).Value = "FHIPO 14";
        });

        var result = await _svc.ParseAndValidateAsync(stream, "gbm.xlsx", MakeCatalog(), 0m);

        Assert.True(result.Success);
        Assert.Empty(result.Errors);
        Assert.Single(result.Positions);
        Assert.Equal("FHIPO14", result.Positions[0].Ticker);
        Assert.Equal(100, result.Positions[0].Titulos);
        Assert.Equal(25.5m, result.Positions[0].CostoPromedio);
    }

    [Fact]
    public async Task ParseAndValidate_PastedFormat_UnknownTickersAreDiscardedSilently()
    {
        var stream = MakeWorkbook(ws =>
        {
            ws.Cell(1, 1).Value = "Emisora";
            ws.Cell(2, 1).Value = "&nbsp;";
            ws.Cell(2, 2).Value = 100;
            ws.Cell(2, 3).Value = 25.5;
            ws.Cell(3, 1).Value = "FHIPO 14";
            ws.Cell(4, 1).Value = string.Empty;
            ws.Cell(4, 2).Value = 50;
            ws.Cell(4, 3).Value = 30.0;
            ws.Cell(5, 1).Value = "FAKE 99";
        });

        var result = await _svc.ParseAndValidateAsync(stream, "gbm.xlsx", MakeCatalog(), 0m);

        Assert.True(result.Success);
        Assert.Empty(result.Errors);
        Assert.Single(result.Positions);
        Assert.Equal("FHIPO14", result.Positions[0].Ticker);
    }

    [Fact]
    public async Task ParseAndValidate_OfficialFormat_DetectedWhenA1IsSectionHeaderInsteadOfEmisoraFondo()
    {
        // GBM sometimes exports without the "Emisora/Fondo" header row at A1,
        // starting directly with a section name like "Mercado de Capitales Global (SIC)".
        var stream = MakeWorkbook(ws =>
        {
            ws.Cell(1, 1).Value = "Mercado de Capitales Global (SIC)";
            ws.Cell(2, 1).Value = "Emisora/Fondo";
            ws.Cell(3, 1).Value = "ANAU N";
            ws.Cell(3, 2).Value = 10;
            ws.Cell(3, 3).Value = "$100.00";
            ws.Cell(4, 1).Value = "IUES N";
            ws.Cell(4, 2).Value = 5;
            ws.Cell(4, 3).Value = "$3,586.00";
        });

        var result = await _svc.ParseAndValidateAsync(stream, "gbm.xlsx", MakeCatalog(), 0m);

        Assert.True(result.Success);
        Assert.Empty(result.Errors);
        Assert.Equal(2, result.Positions.Count);
        Assert.Contains(result.Positions, p => p.Ticker == "ANAUN" && p.Titulos == 10 && p.CostoPromedio == 100m);
        Assert.Contains(result.Positions, p => p.Ticker == "IUESN" && p.CostoPromedio == 3586m);
    }

    [Fact]
    public async Task ParseAndValidate_BasicXlsxHeaders_StillWork()
    {
        var stream = MakeWorkbook(ws =>
        {
            ws.Cell(1, 1).Value = "Ticker";
            ws.Cell(1, 2).Value = "Qty";
            ws.Cell(1, 3).Value = "AvgCost";
            ws.Cell(2, 1).Value = "FUNO11";
            ws.Cell(2, 2).Value = 100;
            ws.Cell(2, 3).Value = 50;
        });

        var result = await _svc.ParseAndValidateAsync(stream, "basic.xlsx", MakeCatalog(), 0m);

        Assert.True(result.Success);
        Assert.Single(result.Positions);
        Assert.Equal("FUNO11", result.Positions[0].Ticker);
    }
}
