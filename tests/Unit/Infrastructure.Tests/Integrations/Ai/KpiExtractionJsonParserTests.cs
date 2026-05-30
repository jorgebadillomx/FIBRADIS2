using System.Reflection;
using Application.Fundamentals;
using Infrastructure.Integrations.Ai;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Integrations.Ai;

public class KpiExtractionJsonParserTests
{
    private static readonly MethodInfo ParseMethod = typeof(GeminiKpiExtractorService)
        .Assembly
        .GetType("Infrastructure.Integrations.Ai.KpiExtractionJsonParser", throwOnError: true)!
        .GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, [typeof(string), typeof(Microsoft.Extensions.Logging.ILogger), typeof(string)])!;

    [Fact]
    public void Parse_ReturnsAllEnrichedFields_OnHappyPath()
    {
        var result = Parse("""
            {
              "capRate": 0.081,
              "capRateNote": "Cap rate explícito.",
              "navPerCbfi": 18.4,
              "navPerCbfiNote": "NAV reportado.",
              "ltv": 0.33,
              "ltvNote": "LTV consolidado.",
              "noiMargin": 0.71,
              "noiMarginNote": "NOI trimestral.",
              "ffoMargin": 0.62,
              "ffoMarginNote": "FFO trimestral.",
              "quarterlyDistribution": 0.48,
              "quarterlyDistributionNote": "Distribución declarada.",
              "summary": "Resumen legacy.",
              "summaryMarkdown": "**Ocupación** sólida.\n\nDeuda controlada.",
              "investorTakeaway": "La distribución parece sostenible.",
              "operationalSignals": ["Ocupación arriba de 95%", "Rentas mismas propiedades positivas"],
              "financialSignals": ["LTV estable", "Liquidez suficiente"],
              "riskFlags": ["Vencimiento relevante en 2027"],
              "extractionNotes": "Se identificaron los campos clave."
            }
            """);

        Assert.True(result.Success);
        Assert.Equal("**Ocupación** sólida.\n\nDeuda controlada.", result.SummaryMarkdown);
        Assert.Equal("La distribución parece sostenible.", result.InvestorTakeaway);
        Assert.Equal(["Ocupación arriba de 95%", "Rentas mismas propiedades positivas"], result.OperationalSignals);
        Assert.Equal(["LTV estable", "Liquidez suficiente"], result.FinancialSignals);
        Assert.Equal(["Vencimiento relevante en 2027"], result.RiskFlags);
    }

    [Fact]
    public void Parse_NormalizesMissingOrNullArrays_ToEmptyLists()
    {
        var result = Parse("""
            {
              "summaryMarkdown": "Resumen suficiente.",
              "operationalSignals": null,
              "financialSignals": [],
              "riskFlags": ["  "],
              "extractionNotes": "Sin KPIs numéricos."
            }
            """);

        Assert.True(result.Success);
        Assert.Equal(Array.Empty<string>(), result.OperationalSignals);
        Assert.Equal(Array.Empty<string>(), result.FinancialSignals);
        Assert.Equal(Array.Empty<string>(), result.RiskFlags);
    }

    [Fact]
    public void Parse_FiltersBlankStrings_FromArrays()
    {
        var result = Parse("""
            {
              "summaryMarkdown": "Resumen suficiente.",
              "operationalSignals": [" ", "Expansión industrial"],
              "financialSignals": ["\t", "Menor costo financiero"],
              "riskFlags": ["", "Cobranza presionada"],
              "extractionNotes": "Texto válido."
            }
            """);

        Assert.Equal(["Expansión industrial"], result.OperationalSignals);
        Assert.Equal(["Menor costo financiero"], result.FinancialSignals);
        Assert.Equal(["Cobranza presionada"], result.RiskFlags);
    }

    private static KpiExtractionResult Parse(string raw)
        => (KpiExtractionResult)ParseMethod.Invoke(null, [raw, NullLogger.Instance, "test"])!;
}
