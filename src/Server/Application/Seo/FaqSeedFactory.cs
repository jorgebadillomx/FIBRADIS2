using Domain.Ops;
using Domain.Seo;

namespace Application.Seo;

public static class FaqSeedFactory
{
    private static readonly DateTimeOffset SeedUpdatedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly IReadOnlyDictionary<string, string> EditorialQuestions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["que-son-las-fibras"] = "¿Qué son las FIBRAs?",
            ["historia"] = "¿Cuál es la historia de las FIBRAs?",
            ["como-se-estructuran"] = "¿Cómo se estructuran las FIBRAs?",
            ["por-que-invertir"] = "¿Por qué invertir en FIBRAs?",
            ["regimen-fiscal"] = "¿Cuál es el régimen fiscal de las FIBRAs?",
        };

    public static IReadOnlyList<FaqItem> BuildEditorialItems(IEnumerable<EditorialPage> pages)
        => pages
            .OrderBy(page => page.Order)
            .Select(page => new FaqItem
            {
                Id = Guid.NewGuid(),
                PageType = SeoPageType.StaticPage,
                EntityKey = GetEditorialEntityKey(),
                Question = EditorialQuestions.TryGetValue(page.Slug, out var question)
                    ? question
                    : page.Title,
                Answer = page.Content.Trim(),
                Order = page.Order,
                IsActive = true,
                UpdatedAt = page.UpdatedAt,
                UpdatedBy = "system",
            })
            .ToList();

    public static IReadOnlyList<FaqItem> BuildFundamentalsItems() =>
    [
        CreateFundamentalsItem(
            1,
            "¿Qué es Cap Rate?",
            $"**Fórmula:** {SharedKpiDefinitions.CapRate.Formula}\n\n{SharedKpiDefinitions.CapRate.Description}"),
        CreateFundamentalsItem(
            2,
            "¿Qué es NAV por CBFI?",
            $"**Fórmula:** {SharedKpiDefinitions.NavPerCbfi.Formula}\n\n{SharedKpiDefinitions.NavPerCbfi.Description}"),
        CreateFundamentalsItem(
            3,
            "¿Qué es LTV?",
            $"**Fórmula:** {SharedKpiDefinitions.Ltv.Formula}\n\n{SharedKpiDefinitions.Ltv.Description}"),
        CreateFundamentalsItem(
            4,
            "¿Qué es NOI Margin?",
            $"**Fórmula:** {SharedKpiDefinitions.NoiMargin.Formula}\n\n{SharedKpiDefinitions.NoiMargin.Description}"),
        CreateFundamentalsItem(
            5,
            "¿Qué es FFO Margin?",
            $"**Fórmula:** {SharedKpiDefinitions.FfoMargin.Formula}\n\n{SharedKpiDefinitions.FfoMargin.Description}"),
        CreateFundamentalsItem(
            6,
            "¿Qué es la distribución trimestral?",
            $"**Fórmula:** {SharedKpiDefinitions.QuarterlyDistribution.Formula}\n\n{SharedKpiDefinitions.QuarterlyDistribution.Description}"),
    ];

    private static FaqItem CreateFundamentalsItem(int order, string question, string answer) => new()
    {
        Id = Guid.NewGuid(),
        PageType = SeoPageType.StaticPage,
        EntityKey = "/fundamentales",
        Question = question,
        Answer = answer.Trim(),
        Order = order,
        IsActive = true,
        UpdatedAt = SeedUpdatedAt,
        UpdatedBy = "system",
    };

    private static string GetEditorialEntityKey() => "/conoce-las-fibras";

    // Copia intencional de los KPI compartidos de frontend: seed inicial de FAQ no debe
    // depender del bundle TS. Si cambia el texto aquí, debe cambiarse también en UI.
    private static class SharedKpiDefinitions
    {
        public static readonly KpiDefinition CapRate = new(
            "Cap Rate",
            "Cap Rate = NOI anualizado / Valor de propiedades de inversión",
            "Tasa de capitalización: mide el rendimiento operativo del portafolio inmobiliario en relación a su valor. Un Cap Rate más alto implica mayor rendimiento y generalmente más riesgo; uno bajo refleja activos premium en alta demanda.");

        public static readonly KpiDefinition NavPerCbfi = new(
            "NAV por CBFI",
            "NAV = Valor de propiedades − Deuda total | NAV/CBFI = NAV / CBFIs en circulación",
            "Valor Neto de los Activos por certificado. Indica si el precio de mercado cotiza con descuento o premio respecto al valor real de los activos que respaldan cada CBFI.");

        public static readonly KpiDefinition Ltv = new(
            "LTV",
            "LTV = Deuda total / Valor de propiedades de inversión",
            "Loan-to-Value: nivel de apalancamiento en relación al valor inmobiliario. LTV bajo indica solidez financiera; LTV alto señala mayor exposición al riesgo.");

        public static readonly KpiDefinition NoiMargin = new(
            "NOI Margin",
            "NOI Margin = NOI / Ingresos Totales",
            "Margen de Ingreso Neto Operativo: porcentaje de ingresos que queda tras descontar gastos directos de operación. Mide la eficiencia operativa del portafolio.");

        public static readonly KpiDefinition FfoMargin = new(
            "FFO Margin",
            "FFO Margin = FFO / Ingresos Totales | FFO = Utilidad Neta + ajustes por valuación − ganancias cambiarias",
            "Fondos de Operación sobre ingresos. El FFO corrige la utilidad neta eliminando distorsiones contables para mostrar cuánto genera realmente el portafolio en operación.");

        public static readonly KpiDefinition QuarterlyDistribution = new(
            "Dist. Trimestral",
            "Distribución = Resultado Fiscal Distribuido + Reembolso de Capital",
            "Pago en efectivo por CBFI cada trimestre. Puede componerse de utilidades fiscales y reembolso de capital.");
    }

    private sealed record KpiDefinition(string Label, string Formula, string Description);
}
