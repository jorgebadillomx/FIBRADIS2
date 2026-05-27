using Domain.News;

namespace Infrastructure.Integrations.Ai;

public static class AiPromptTemplateDefaults
{
    public const string NewsContentType = "news";
    public const string KpiExtractionContentType = "kpi_extraction";

    public static string GetTemplate(string contentType)
        => string.Equals(contentType, KpiExtractionContentType, StringComparison.OrdinalIgnoreCase)
            ? KpiExtraction
            : News;

    public const string News = """
        Eres un analista experto en FIBRAs mexicanas (Fideicomisos de Inversión en Bienes Raíces) con amplio conocimiento del mercado inmobiliario y bursátil de México.
        {strictness_instruction}
        Título: {title}
        {snippet_section}
        {body_section}
        Incluye: el hecho central, su relevancia para el sector de FIBRAs o bienes raíces en México, los datos más materiales del artículo cuando existan, y una lectura analítica breve para el inversionista.
        Si el artículo contiene cifras, fechas, montos, porcentajes, dividendos, emisiones, ocupación, adquisiciones o guidance, intégralos en el resumen.
        No escribas menos de 5 oraciones si el cuerpo del artículo está disponible. Responde solo con el resumen, sin preámbulos.
        """;

    public const string KpiExtraction = """
        Eres un analista experto en FIBRAs mexicanas (Fideicomisos de Inversión en Bienes Raíces). Tu tarea es extraer KPIs financieros clave de un reporte trimestral o anual en formato markdown y devolver ÚNICAMENTE un objeto JSON con la siguiente estructura (sin texto adicional, sin bloques de código):

        {
          "capRate": <número decimal o null>,
          "capRateNote": "<de dónde proviene el dato, o por qué es null>",
          "navPerCbfi": <número decimal o null>,
          "navPerCbfiNote": "<nota>",
          "ltv": <número decimal o null>,
          "ltvNote": "<nota>",
          "noiMargin": <número decimal o null>,
          "noiMarginNote": "<nota>",
          "ffoMargin": <número decimal o null>,
          "ffoMarginNote": "<nota>",
          "quarterlyDistribution": <número decimal o null>,
          "quarterlyDistributionNote": "<nota>",
          "summary": "<resumen analítico de 4-6 oraciones sobre el desempeño financiero de la FIBRA en el período: hechos clave, tendencias, fortalezas y riesgos>",
          "extractionNotes": "<observaciones generales sobre la calidad o limitaciones de la extracción>"
        }

        Reglas:
        - Los valores numéricos son números puros (sin símbolo de moneda, sin comas de miles, sin %).
        - capRate, ltv, noiMargin, ffoMargin son porcentajes: si el reporte los expresa como "8.5%", devuelve 0.085.
        - quarterlyDistribution es la distribución por CBFI en pesos (valor absoluto, no porcentaje).
        - navPerCbfi es el NAV por CBFI en pesos.
        - Si un KPI puede calcularse con certeza a partir de los datos del reporte, calcúlalo e indícalo en la nota.
        - Si un KPI no puede determinarse, devuelve null y explica brevemente en la nota.
        - Las notas deben ser concisas (máximo 2 oraciones).
        - Devuelve ÚNICAMENTE el objeto JSON, sin texto introductorio.

        Reporte:
        {markdown_content}
        """;

    public static string ResolveContentType(AiContentType contentType)
        => contentType == AiContentType.Document ? KpiExtractionContentType : NewsContentType;
}
