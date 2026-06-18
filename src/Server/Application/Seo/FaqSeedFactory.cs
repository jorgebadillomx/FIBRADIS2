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

    public static IReadOnlyList<FaqItem> BuildStaticPagesItems() =>
    [
        // /fibras — Catálogo
        CreateStaticItem("/fibras", 1,
            "¿Cuántas FIBRAs inmobiliarias existen en México?",
            "En México cotizan alrededor de 14 FIBRAs activas en la Bolsa Mexicana de Valores (BMV). El catálogo de Fibras Inmobiliarias muestra el universo completo con precio, sector, yield y métricas fundamentales actualizadas."),
        CreateStaticItem("/fibras", 2,
            "¿Cómo se clasifican las FIBRAs por sector?",
            "Las FIBRAs se agrupan por tipo de activo inmobiliario: Industrial/Logístico (bodegas, parques industriales), Comercial (centros comerciales, retail), Oficinas, Diversificado (mezcla de activos) y Especializado (educación, salud, almacenamiento). El sector determina en gran medida su comportamiento ante ciclos económicos."),
        CreateStaticItem("/fibras", 3,
            "¿Qué información muestra el catálogo de FIBRAs?",
            "Cada ficha del catálogo incluye: precio actual, cambio del día, promedio 52 semanas, Cap Rate, LTV, NAV por CBFI, Margen NOI, yield calculado y el Score de oportunidad. Los datos de mercado se actualizan en tiempo real vía Yahoo Finance; los fundamentales provienen de los reportes trimestrales oficiales."),

        // /comparar — Comparador
        CreateStaticItem("/comparar", 1,
            "¿Cómo comparar FIBRAs inmobiliarias de forma objetiva?",
            "El comparador de Fibras Inmobiliarias permite contrastar hasta 4 emisoras en una sola tabla: precio, rendimiento, Cap Rate, LTV, Margen NOI/FFO y Score de oportunidad. Cada métrica resalta automáticamente al mejor valor y muestra el margen respecto al segundo lugar."),
        CreateStaticItem("/comparar", 2,
            "¿Qué métricas son las más importantes al comparar FIBRAs?",
            "Las métricas clave son: **Yield** (distribución anual / precio de mercado), **Cap Rate** (rendimiento operativo del portafolio), **LTV** (nivel de deuda vs valor de activos) y **NAV Descuento** (precio de mercado vs valor intrínseco). Un yield alto con LTV bajo y Cap Rate sólido generalmente indica una oportunidad más favorable."),
        CreateStaticItem("/comparar", 3,
            "¿Cuántas FIBRAs puedo comparar al mismo tiempo?",
            "Puedes comparar de 2 a 4 FIBRAs simultáneamente. La selección se guarda en la URL, por lo que puedes compartir o guardar la comparación directamente desde el navegador."),

        // /noticias — Noticias
        CreateStaticItem("/noticias", 1,
            "¿Dónde encontrar noticias y comunicados de FIBRAs mexicanas?",
            "La sección de noticias de Fibras Inmobiliarias agrega reportes trimestrales, comunicados de asamblea, anuncios de distribución y cobertura de medios especializados para cada FIBRA. Los artículos están vinculados a la ficha de cada emisora para dar contexto al momento."),
        CreateStaticItem("/noticias", 2,
            "¿Con qué frecuencia se publican resultados trimestrales de FIBRAs?",
            "Las FIBRAs reportan resultados cada trimestre: Q1 (marzo-abril), Q2 (julio-agosto), Q3 (octubre-noviembre) y Q4 (febrero-marzo del año siguiente). Los reportes incluyen NOI, FFO, distribuciones decretadas y actualización de la valuación del portafolio."),

        // /calculadora — Calculadora
        CreateStaticItem("/calculadora", 1,
            "¿Cómo calcular el rendimiento esperado de una FIBRA?",
            "Ingresa el monto a invertir y el precio actual del CBFI. La calculadora estima las distribuciones trimestrales proyectadas según el yield histórico de cada FIBRA, el número de CBFIs que recibirías y el rendimiento anualizado. Recuerda que las distribuciones pasadas no garantizan rendimientos futuros."),
        CreateStaticItem("/calculadora", 2,
            "¿Qué es el yield de una FIBRA y cómo se calcula?",
            "El yield (o rendimiento por distribución) es la distribución anual por CBFI dividida entre el precio de mercado actual. Ejemplo: si una FIBRA distribuye $0.80 MXN por CBFI trimestralmente ($3.20 anuales) y cotiza a $40 MXN, su yield es 8%. Un yield más alto puede indicar mayor valor, pero conviene validarlo con el LTV y Cap Rate."),
        CreateStaticItem("/calculadora", 3,
            "¿Qué diferencia hay entre yield calculado y yield decretado?",
            "El **yield calculado** usa la última distribución trimestral anualizada (×4) sobre el precio actual, reflejando el rendimiento al precio de hoy. El **yield decretado** suma las distribuciones efectivamente pagadas en los últimos 12 meses. La diferencia es relevante cuando una FIBRA ha cambiado su política de distribución recientemente."),

        // /calendario — Calendario
        CreateStaticItem("/calendario", 1,
            "¿Cuándo pagan distribuciones las FIBRAs inmobiliarias?",
            "La mayoría de las FIBRAs distribuyen cada trimestre, típicamente en enero, abril, julio y octubre, aunque las fechas exactas varían por emisora. El calendario de Fibras Inmobiliarias centraliza las fechas de decreto, registro y pago de todas las FIBRAs activas para que no pierdas ningún cobro."),
        CreateStaticItem("/calendario", 2,
            "¿Qué es la fecha de registro en las distribuciones de FIBRAs?",
            "La fecha de registro (o *record date*) es el día en que debes tener los CBFIs en tu cuenta para tener derecho a la distribución. Comprar después de esa fecha implica que no recibirás el pago del trimestre en curso. El calendario incluye fecha de decreto, fecha de registro y fecha estimada de pago."),
        CreateStaticItem("/calendario", 3,
            "¿Cómo saber cuánto voy a cobrar de distribución?",
            "Multiplica el monto decretado por CBFI por el número de CBFIs que tienes. Por ejemplo: si tienes 500 CBFIs y la distribución decretada es $0.85 MXN/CBFI, recibirás $425 MXN brutos ese trimestre. La calculadora de Fibras Inmobiliarias puede proyectar este cálculo para cualquier monto de inversión."),
    ];

    private static FaqItem CreateStaticItem(string entityKey, int order, string question, string answer) => new()
    {
        Id = Guid.NewGuid(),
        PageType = SeoPageType.StaticPage,
        EntityKey = entityKey,
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
