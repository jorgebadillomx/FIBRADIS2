using ClosedXML.Excel;
using Domain.Catalog;
using Infrastructure.Portfolio;

namespace Infrastructure.Tests.Portfolio;

public class PortfolioUploadServiceTests
{
    private static readonly Guid FunoId = Guid.NewGuid();
    private static readonly Guid FihoId = Guid.NewGuid();
    private static readonly Guid VestaId = Guid.NewGuid();

    private static IReadOnlyList<Fibra> MakeCatalog() =>
    [
        MakeFibra("FUNO11", FunoId),
        MakeFibra("FIHO12", FihoId),
        MakeFibra("VESTA15", VestaId),
    ];

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

    private static Stream MakeXlsxStream(string[,] data)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Data");
        for (var r = 0; r < data.GetLength(0); r++)
            for (var c = 0; c < data.GetLength(1); c++)
                ws.Cell(r + 1, c + 1).Value = data[r, c];
        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    private readonly PortfolioUploadService _svc = new();

    [Fact]
    public async Task ParseAndValidate_ValidXlsx_ReturnsPositions()
    {
        var stream = MakeXlsxStream(new[,]
        {
            { "Ticker", "Qty", "AvgCost" },
            { "FUNO11", "100", "50.00" },
            { "FIHO12", "200", "25.50" },
            { "VESTA15", "300", "35.00" },
            { "FUNO11", "50", "48.00" },  // consolidado con la primera
            { "FIHO12", "100", "26.00" }, // consolidado con la segunda
        });

        var result = await _svc.ParseAndValidateAsync(stream, "test.xlsx", MakeCatalog(), 0m);

        Assert.True(result.Success);
        Assert.Empty(result.Errors);
        Assert.Equal(3, result.Positions.Count); // 3 tickers únicos consolidados
    }

    [Fact]
    public async Task ParseAndValidate_InvalidTicker_ReturnsRowError()
    {
        var stream = MakeXlsxStream(new[,]
        {
            { "Ticker", "Qty", "AvgCost" },
            { "FUNO11", "100", "50.00" },
            { "FAKEXX", "200", "25.00" },
        });

        var result = await _svc.ParseAndValidateAsync(stream, "test.xlsx", MakeCatalog(), 0m);

        Assert.False(result.Success);
        Assert.Single(result.Errors);
        Assert.Equal(2, result.Errors[0].RowNumber);
        Assert.Equal("FAKEXX", result.Errors[0].Ticker);
        Assert.Contains("catálogo", result.Errors[0].Message);
    }

    [Fact]
    public async Task ParseAndValidate_DuplicateTickers_Consolidates()
    {
        var stream = MakeXlsxStream(new[,]
        {
            { "Ticker", "Qty", "AvgCost" },
            { "FUNO11", "500", "47.00" },
            { "FUNO11", "300", "45.00" },
        });

        var result = await _svc.ParseAndValidateAsync(stream, "test.xlsx", MakeCatalog(), 0m);

        Assert.True(result.Success);
        Assert.Single(result.Positions);
        var pos = result.Positions[0];
        Assert.Equal(800, pos.Titulos);
        // Promedio ponderado: (500*47 + 300*45) / 800 = (23500 + 13500) / 800 = 37000 / 800 = 46.25
        Assert.Equal(46.25m, Math.Round(pos.CostoPromedio, 2));
    }

    [Fact]
    public async Task ParseAndValidate_WrongHeaders_ReturnsSingleHeaderError()
    {
        var stream = MakeXlsxStream(new[,]
        {
            { "Ticker", "Qty", "Cost" }, // "Cost" en vez de "AvgCost"
            { "FUNO11", "100", "50.00" },
        });

        var result = await _svc.ParseAndValidateAsync(stream, "test.xlsx", MakeCatalog(), 0m);

        Assert.False(result.Success);
        Assert.Single(result.Errors);
        Assert.Contains("AvgCost", result.Errors[0].Message);
    }

    [Fact]
    public async Task ParseAndValidate_NegativeQty_ReturnsRowError()
    {
        var stream = MakeXlsxStream(new[,]
        {
            { "Ticker", "Qty", "AvgCost" },
            { "FUNO11", "-50", "50.00" },
        });

        var result = await _svc.ParseAndValidateAsync(stream, "test.xlsx", MakeCatalog(), 0m);

        Assert.False(result.Success);
        Assert.Single(result.Errors);
        Assert.Contains("Qty", result.Errors[0].Message);
    }

    [Fact]
    public async Task ParseAndValidate_CommissionFactor_CalculatesCostoTotalCompra()
    {
        var stream = MakeXlsxStream(new[,]
        {
            { "Ticker", "Qty", "AvgCost" },
            { "FUNO11", "1000", "50.00" },
        });

        // CostoTotal = 1000 * 50 * (1 + 0.006) = 50000 * 1.006 = 50300
        var result = await _svc.ParseAndValidateAsync(stream, "test.xlsx", MakeCatalog(), 0.006m);

        Assert.True(result.Success);
        Assert.Single(result.Positions);
        Assert.Equal(50300m, result.Positions[0].CostoTotalCompra);
    }

    [Fact]
    public async Task ParseAndValidate_CsvLowercaseHeaders_ParsesSuccessfully()
    {
        var csv = "ticker,qty,avgcost\nFUNO11,100,50.00\n";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csv));

        var result = await _svc.ParseAndValidateAsync(stream, "test.csv", MakeCatalog(), 0m);

        Assert.True(result.Success);
        Assert.Single(result.Positions);
        Assert.Equal("FUNO11", result.Positions[0].Ticker);
    }

    [Fact]
    public async Task ParseAndValidate_EmptyFile_ReturnsError()
    {
        var stream = MakeXlsxStream(new[,] { { "Ticker", "Qty", "AvgCost" } });

        var result = await _svc.ParseAndValidateAsync(stream, "test.xlsx", MakeCatalog(), 0m);

        Assert.False(result.Success);
        Assert.Single(result.Errors);
        Assert.Contains("posiciones", result.Errors[0].Message);
    }

    [Fact]
    public async Task ParseAndValidate_UnsupportedExtension_ReturnsError()
    {
        var stream = new MemoryStream([]);

        var result = await _svc.ParseAndValidateAsync(stream, "file.xls", MakeCatalog(), 0m);

        Assert.False(result.Success);
        Assert.Single(result.Errors);
        Assert.Contains("no soportado", result.Errors[0].Message);
    }
}
