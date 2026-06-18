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
        // /fibras — Página principal / universo de FIBRAs
        CreateStaticItem("/fibras", 1,
            "¿Qué información muestra la tabla del universo de FIBRAs?",
            "La tabla muestra todas las FIBRAs activas con sus columnas principales: Precio, variación del día en pesos y porcentaje, Volumen, Máximo y Mínimo de las últimas 52 semanas, Yield anualizado y el período del último reporte. Puedes ordenar por cualquier columna y filtrar por ticker."),
        CreateStaticItem("/fibras", 2,
            "¿Qué son los Ganadores y Perdedores del día?",
            "Son los top 5 de FIBRAs con mayor y menor variación porcentual en la sesión actual. Se actualiza conforme avanza la jornada bursátil y permite identificar de un vistazo qué emisoras están teniendo el mejor y peor desempeño en precio ese día."),
        CreateStaticItem("/fibras", 3,
            "¿Qué es el Yield que aparece en el catálogo?",
            "Es el rendimiento anualizado estimado a partir de la última distribución trimestral reportada, dividida entre el precio de mercado actual. Se calcula como: (distribución trimestral × 4) / precio actual. Cambia en tiempo real con el precio y sirve como referencia rápida de rentabilidad por distribución."),

        // /comparar — Comparador
        CreateStaticItem("/comparar", 1,
            "¿Qué secciones incluye el comparador de FIBRAs?",
            "El comparador organiza la información en cuatro secciones: **Mercado** (precio, cambio del día, promedio 52 semanas, volumen), **Fundamentales** (Cap Rate, NAV por CBFI, LTV, Margen NOI y Margen FFO), **Distribuciones** (distribución trimestral, yield calculado y yield decretado) y **Score público** (score de oportunidad y sus cinco componentes)."),
        CreateStaticItem("/comparar", 2,
            "¿Cómo indica el comparador cuál FIBRA gana en cada métrica?",
            "La celda con el mejor valor de cada fila se resalta en verde. Debajo del valor aparece la diferencia respecto al segundo lugar, por ejemplo '+1.3 pp vs FUNO' para métricas de porcentaje o '+8.4 pts vs MXCD' para el score. En métricas donde menor es mejor (como LTV), el resalte corresponde al valor más bajo."),
        CreateStaticItem("/comparar", 3,
            "¿Puedo compartir una comparación específica?",
            "Sí. Cada selección de FIBRAs queda reflejada en la URL de la página. Para compartir o guardar una comparación, copia el enlace del navegador; al abrirlo, cargará exactamente las mismas emisoras comparadas."),

        // /noticias — Noticias
        CreateStaticItem("/noticias", 1,
            "¿Cómo buscar noticias de una FIBRA en particular?",
            "Usa el filtro 'Filtrar por FIBRA' para ver solo las noticias asociadas a una emisora específica. También puedes combinar ese filtro con la búsqueda por título para afinar más los resultados. El botón 'Limpiar filtros' restablece ambos criterios."),
        CreateStaticItem("/noticias", 2,
            "¿Qué datos muestra cada nota en el listado de noticias?",
            "Cada tarjeta incluye la fuente de la noticia, el tiempo relativo de publicación, el titular, un extracto del contenido y etiquetas con las FIBRAs a las que está vinculada. Si hay más de las que caben, se muestra '+N más'. Al hacer clic entras al detalle completo de la nota."),

        // /calculadora — Calculadora de compra
        CreateStaticItem("/calculadora", 1,
            "¿Cómo funciona la calculadora de compra de FIBRAs?",
            "Ingresa un monto en pesos en la columna '$ a calcular' para cada FIBRA que te interese. La tabla calcula al instante cuántos CBFIs puedes comprar, cuánto dinero sobra, la distribución proyectada por CBFI (trimestral y anual) y la renta bruta estimada sobre tu inversión. Puedes ingresar montos distintos para cada emisora."),
        CreateStaticItem("/calculadora", 2,
            "¿Qué significa '$ Sobra' en la calculadora?",
            "Es el remanente de tu presupuesto que no alcanza para comprar un CBFI adicional, dado que los CBFIs se compran en unidades enteras. Por ejemplo, si el precio es $47.30 MXN y tu presupuesto es $500, puedes comprar 10 CBFIs ($473) y te sobran $27."),
        CreateStaticItem("/calculadora", 3,
            "¿Los montos se pierden al filtrar u ordenar la tabla?",
            "No. La tabla conserva los montos ingresados aunque cambies el orden de las columnas o apliques un filtro por nombre. Esto permite ordenar por Renta Bruta o Yield sin perder los presupuestos que ya capturaste."),

        // /calendario — Calendario de eventos corporativos
        CreateStaticItem("/calendario", 1,
            "¿Qué tipos de eventos muestra el calendario de FIBRAs?",
            "El calendario registra tres tipos de eventos: **Pagos** (fecha en que se deposita la distribución), **Ex derechos** (último día para comprar y tener derecho al pago) y **Avisos BMV** (comunicados oficiales publicados en la Bolsa Mexicana de Valores). Cada tipo tiene un color de identificación distinto."),
        CreateStaticItem("/calendario", 2,
            "¿Qué información aparece al revisar un evento en el calendario?",
            "Cada evento muestra la emisora, el monto de distribución por CBFI, el desglose entre la parte fiscal y el reembolso de capital, y un enlace al aviso oficial en BMV cuando está disponible. La vista mensual agrupa los eventos por día para lectura rápida."),
        CreateStaticItem("/calendario", 3,
            "¿Qué es la fecha ex derecho y por qué importa?",
            "Es la fecha a partir de la cual quien compre la FIBRA ya no tiene derecho a cobrar la distribución en curso. Para recibir el pago debes tener los CBFIs registrados en tu cuenta **antes** de esa fecha. El mismo día puede aparecer tanto el pago de una distribución como la fecha ex derecho de la siguiente."),
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
