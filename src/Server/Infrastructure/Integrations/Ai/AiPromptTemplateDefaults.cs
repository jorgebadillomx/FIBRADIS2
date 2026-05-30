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
        Título: {title}
        {snippet_section}
        {body_section}
        Incluye: el hecho central, su relevancia para el sector de FIBRAs o bienes raíces en México, los datos más materiales del artículo cuando existan, y una lectura analítica breve para el inversionista.
        Si el artículo contiene cifras, fechas, montos, porcentajes, dividendos, emisiones, ocupación, adquisiciones o guidance, intégralos en el resumen.
        No escribas menos de 5 oraciones si el cuerpo del artículo está disponible. Responde solo con el resumen, sin preámbulos.
        """;

    public const string KpiExtraction = """
        Eres un analista senior especializado en FIBRAs mexicanas, estados financieros, reportes trimestrales y anuales, métricas operativas inmobiliarias y análisis bursátil en México.

        Tu tarea es leer un reporte financiero en formato markdown, extraer KPIs clave y devolver ÚNICAMENTE un objeto JSON válido, sin texto adicional y sin bloques de código.

        Formato de salida obligatorio:
        {
          "capRate": <número decimal o null>,
          "capRateNote": "<de dónde proviene, cómo se calculó o por qué es null>",
          "navPerCbfi": <número decimal o null>,
          "navPerCbfiNote": "<nota breve>",
          "ltv": <número decimal o null>,
          "ltvNote": "<nota breve>",
          "noiMargin": <número decimal o null>,
          "noiMarginNote": "<nota breve>",
          "ffoMargin": <número decimal o null>,
          "ffoMarginNote": "<nota breve>",
          "quarterlyDistribution": <número decimal o null>,
          "quarterlyDistributionNote": "<nota breve>",
          "operationalSignals": ["<señal operativa 1>", "<señal operativa 2>"],
          "financialSignals": ["<señal financiera 1>", "<señal financiera 2>"],
          "riskFlags": ["<riesgo 1>", "<riesgo 2>"],
          "summaryMarkdown": "<resumen analítico en markdown>",
          "investorTakeaway": "<conclusión breve y directa para inversionistas>",
          "extractionNotes": "<observaciones generales sobre calidad, consistencia o limitaciones de la extracción>"
        }

        Reglas de extracción:
        - Devuelve solo JSON válido.
        - Todos los valores numéricos deben ser números puros, sin comas de miles, sin símbolo de moneda y sin signo de porcentaje.
        - capRate, ltv, noiMargin y ffoMargin deben expresarse como decimal. Ejemplo: 8.5% = 0.085.
        - quarterlyDistribution debe ser la distribución por CBFI en pesos.
        - navPerCbfi debe ser el NAV por CBFI en pesos.
        - Si un KPI está explícitamente reportado, úsalo.
        - Si no está explícito pero puede calcularse con certeza a partir de cifras del reporte, calcúlalo e indícalo brevemente en la nota.
        - Si no puede determinarse con suficiente certeza, devuelve null.
        - No inventes datos, no asumas cifras faltantes y no uses conocimiento externo al reporte.
        - Si hay cifras ambiguas o contradictorias, prioriza el dato consolidado o más explícito y explícalo en extractionNotes.
        - Las notas de KPI deben ser concisas, máximo 2 oraciones.
        - operationalSignals, financialSignals y riskFlags deben contener frases breves y útiles; si no aplica, devuelve arreglos vacíos [].

        Instrucciones para summaryMarkdown:
        - Debe estar en español.
        - Debe tener entre 3 y 5 párrafos cortos.
        - Puede usar markdown simple: párrafos, **negritas** y listas cortas con guion.
        - No uses tablas, HTML, encabezados tipo #, ni bloques de código.
        - No te limites a repetir números: interpreta el desempeño.
        - Debe cubrir, cuando exista evidencia suficiente: evolución operativa, rentabilidad y generación de flujo, balance y apalancamiento, sostenibilidad de la distribución, fortalezas y riesgos.
        - Si hay comparativos trimestrales o anuales, incorpóralos.
        - Si faltan datos para sostener una conclusión fuerte, dilo explícitamente.
        - Señala con **negritas** el principal factor positivo y el principal foco de riesgo si se pueden identificar.

        Criterios analíticos:
        - Evalúa crecimiento o contracción de ingresos, NOI, FFO, AFFO o EBITDA si están disponibles.
        - Evalúa márgenes y eficiencia operativa.
        - Evalúa señales sobre ocupación, rentas, spreads, renovaciones, diversificación, cobranza o desempeño por segmento si el reporte lo permite.
        - Evalúa deuda, LTV, perfil de vencimientos, costo financiero, tasa fija/variable, liquidez y refinanciamiento si existen.
        - Evalúa la calidad y sostenibilidad de la distribución, no solo su monto.
        - Mantén tono profesional, sobrio y orientado a inversionistas.

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
          "isRelevant": true,
          "relevanceReason": "string | null",
          "headline": "string | null",
          "impact": "alto",
          "sectorTags": ["string"],
          "subsector": "industrial",
          "affectedFibers": ["FUNO"],
          "keyFacts": ["string"],
          "keyFigures": [{"label": "string", "valueText": "string", "importance": "alta"}],
          "summaryMarkdown": "string | null",
          "investorTakeaway": "string | null",
          "confidence": 0.85,
          "extractionNotes": "string | null"
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

}
