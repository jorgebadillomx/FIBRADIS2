using Domain.News;

namespace Infrastructure.Integrations.Ai;

public static class AiPromptTemplateDefaults
{
    public const string NewsContentType = "news";
    public const string KpiExtractionContentType = "kpi_extraction";
    public const string NewsAnalysisContentType = "news_analysis";

    public static string GetTemplate(string contentType)
    {
        if (string.Equals(contentType, KpiExtractionContentType, StringComparison.OrdinalIgnoreCase))
            return KpiExtraction;
        if (string.Equals(contentType, NewsAnalysisContentType, StringComparison.OrdinalIgnoreCase))
            return NewsAnalysis;
        return News;
    }

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

    public const string NewsAnalysis = """
        Eres un analista experto en FIBRAs mexicanas (Fideicomisos de Inversión en Bienes Raíces) con amplio conocimiento del mercado inmobiliario y bursátil de México.

        Tu tarea es analizar la siguiente noticia y devolver ÚNICAMENTE un objeto JSON con la estructura indicada. No uses bloques de código markdown. No incluyas texto antes o después del JSON.

        Título: {title}
        {snippet_section}
        {body_section}

        Devuelve exactamente este JSON (sin texto adicional):
        {
          "isRelevant": true o false,
          "relevanceReason": "Razón breve de por qué la noticia es o no es relevante para FIBRAs mexicanas o el mercado inmobiliario de México",
          "headline": "Titular analítico breve y distinto al título original, o null si no aplica",
          "impact": "alto, medio, bajo o nulo",
          "sectorTags": ["retail", "industrial"],
          "subsector": "industrial, oficinas, comercial, hotelero, residencial, logistico, educativo, salud, mixto, otro, o null",
          "affectedFibers": ["FUNO", "FIBRAMQ"],
          "keyFacts": ["Hecho material 1", "Hecho material 2"],
          "keyFigures": [{"label": "Distribución por CBFI", "valueText": "$0.47", "importance": "alta, media o baja"}],
          "summaryMarkdown": "Resumen analítico en markdown (5-7 oraciones). Null si la noticia no es relevante.",
          "investorTakeaway": "Conclusión breve y directa para inversionistas, o null si no aplica",
          "confidence": 0.85,
          "extractionNotes": "Limitaciones o ambigüedades, o null si no las hay"
        }

        Reglas obligatorias:
        - Responde ÚNICAMENTE con el JSON. No uses bloques de código markdown (no uses ```json).
        - impact debe ser exactamente uno de: alto, medio, bajo, nulo.
        - subsector debe ser exactamente uno de: industrial, oficinas, comercial, hotelero, residencial, logistico, educativo, salud, mixto, otro, o null.
        - affectedFibers debe contener solo tickers de FIBRAs mexicanas reales (FUNO, FIBRAMQ, FIBRAPL, TERRA, FMTY, DANHOS, FNOVA, FIHO, HGLSI, etc.) que se mencionen explícitamente en el artículo.
        - Si un campo no aplica, usa null para strings/objetos o [] para arrays.
        - confidence es un número decimal entre 0 y 1 que refleja tu certeza de extracción, no la calidad de la noticia.
        - Si isRelevant es false, impact debe ser "nulo".
        - keyFigures solo debe incluir cifras explícitas y concretas del artículo (montos, porcentajes, fechas financieras).
        """;

    public static string ResolveContentType(AiContentType contentType)
        => contentType == AiContentType.Document ? KpiExtractionContentType : NewsContentType;
}
