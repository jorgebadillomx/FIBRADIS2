using Infrastructure.Integrations.PdfDiscovery;

namespace Infrastructure.Tests.Integrations.PdfDiscovery;

public class AmefibraTitleParserTests
{
    [Theory]
    [InlineData("2022 Reporte T4 FUNO", "Q4-2022", "quarterly", "eligible", "funo")]
    [InlineData("2021 Reporte T3 Fibra Inn", "Q3-2021", "quarterly", "eligible", "fibra inn")]
    public void Parse_WithQuarterlyTitle_NormalizesPeriod(string title, string period, string reportType, string status, string fibraHint)
    {
        var result = AmefibraTitleParser.Parse(title);

        Assert.Equal(period, result.Period);
        Assert.Equal(reportType, result.ReportType);
        Assert.Equal(status, result.DiscoveryStatus);
        Assert.Equal(fibraHint, result.FibraHint);
    }

    [Fact]
    public void Parse_WithAnnualTitle_ClassifiesAsAnnual()
    {
        var result = AmefibraTitleParser.Parse("2020 Reporte Anual Terrafina (Bolsa de valores)");

        Assert.Null(result.Period);
        Assert.Equal("annual", result.ReportType);
        Assert.Equal("annual", result.DiscoveryStatus);
        Assert.Equal("terrafina", result.FibraHint);
    }

    [Fact]
    public void Parse_WithAmbiguousTitle_ReturnsPendingClassification()
    {
        var result = AmefibraTitleParser.Parse("Reporte especial Fibra MQ");

        Assert.Null(result.Period);
        Assert.Equal("unknown", result.ReportType);
        Assert.Equal("pending-classification", result.DiscoveryStatus);
        Assert.NotNull(result.ErrorReason);
    }

    [Fact]
    public void NormalizeDownloadSignature_RemovesRefreshParameter()
    {
        var result = AmefibraTitleParser.NormalizeDownloadSignature(
            "https://amefibra.com/download/reporte-trimestral-4t-2022-funo/?wpdmdl=2735&refresh=6a1c8e543596f1780256340");

        Assert.Equal("https://amefibra.com/download/reporte-trimestral-4t-2022-funo/?wpdmdl=2735", result);
    }
}
