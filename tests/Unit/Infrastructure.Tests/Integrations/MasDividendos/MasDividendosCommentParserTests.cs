using Infrastructure.Integrations.MasDividendos;

namespace Infrastructure.Tests.Integrations.MasDividendos;

public class MasDividendosCommentParserTests
{
    [Theory]
    [MemberData(nameof(PercentageCases))]
    public void Parse_PercentagePattern_UsesTotalAmount(string comment, decimal totalAmount, decimal? expectedTaxable, decimal? expectedCapital)
    {
        var (taxable, capital) = MasDividendosCommentParser.Parse(comment, totalAmount);

        Assert.Equal(expectedTaxable, taxable);
        Assert.Equal(expectedCapital, capital);
    }

    [Fact]
    public void Parse_DollarLabelPattern_AccumulatesExplicitAmounts()
    {
        var (taxable, capital) = MasDividendosCommentParser.Parse("$0.30 Resultado fiscal; $0.20 Reembolso de capital", 10m);

        Assert.Equal(0.30m, taxable);
        Assert.Equal(0.20m, capital);
    }

    [Fact]
    public void Parse_LabelAmountPattern_ParsesLabelBeforeAmount()
    {
        var (taxable, capital) = MasDividendosCommentParser.Parse("Resultado fiscal: $0.45, Reembolso de capital: 0.15", 10m);

        Assert.Equal(0.45m, taxable);
        Assert.Equal(0.15m, capital);
    }

    [Fact]
    public void Parse_SimpleLabelPattern_AssignsTotalAmount()
    {
        var (taxable, capital) = MasDividendosCommentParser.Parse("CUFIN 2025", 1.25m);

        Assert.Equal(1.25m, taxable);
        Assert.Null(capital);
    }

    [Fact]
    public void Parse_NormalizesBrokenEncodingBeforeParsing()
    {
        var (taxable, capital) = MasDividendosCommentParser.Parse("DistribuciÃ³n de intereses: $0.77", 10m);

        Assert.Equal(0.77m, taxable);
        Assert.Null(capital);
    }

    public static IEnumerable<object[]> UnclassifiedCases()
    {
        yield return ["Concepto a confirmar"];
        yield return ["concepto no mencionado"];
        yield return ["Pendiente de confirmar"];
        yield return ["por confirmar"];
    }

    [Theory]
    [MemberData(nameof(UnclassifiedCases))]
    public void Parse_UnclassifiedComments_ReturnNulls(string comment)
    {
        var (taxable, capital) = MasDividendosCommentParser.Parse(comment, 10m);

        Assert.Null(taxable);
        Assert.Null(capital);
    }

    [Fact]
    public void Parse_PercentagePattern_NullTotal_ReturnsNulls()
    {
        var (taxable, capital) = MasDividendosCommentParser.Parse("100% Resultado fiscal", null);

        Assert.Null(taxable);
        Assert.Null(capital);
    }

    public static IEnumerable<object[]> PercentageCases()
    {
        yield return ["50% Resultado fiscal, 50% Reembolso de capital", 10m, 5m, 5m];
        yield return ["25% Resultado fiscal; 75% Reembolso de capital", 20m, 5m, 15m];
        yield return ["100% Resultado fiscal", 1.25m, 1.25m, null!];
        yield return ["100% Reembolso de capital", 0.80m, null!, 0.80m];
        yield return ["40% CUFIN; 60% Reembolso de capital", 1.00m, 0.40m, 0.60m];
        yield return ["33,33% Resultado fiscal; 33,33% CUCA; 33,34% Reembolso de capital", 3.00m, 0.9999m, 2.0001m];
    }
}
