using Domain.News;

namespace Infrastructure.Integrations.Ai;

public static class AiPromptTemplateDefaults
{
    public const string NewsContentType = "news";
    public const string DocumentContentType = "document";

    public static string GetTemplate(string contentType)
        => string.Equals(contentType, DocumentContentType, StringComparison.OrdinalIgnoreCase)
            ? Document
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

    public const string Document = """
        Eres un analista experto en FIBRAs mexicanas y documentos financieros corporativos del sector inmobiliario en México.
        {strictness_instruction}
        Título: {title}
        {snippet_section}
        {body_section}
        Resume el hecho central del documento, su relevancia para fundamentales, los datos cuantitativos más materiales y una lectura analítica breve para un inversionista.
        Si el documento contiene cifras, guidance, rentas, NOI, AFFO, FFO, ocupación, adquisiciones, deuda o cap rates, intégralos en el resumen.
        No escribas menos de 5 oraciones si el cuerpo del documento está disponible. Responde solo con el resumen, sin preámbulos.
        """;

    public static string ResolveContentType(AiContentType contentType)
        => contentType == AiContentType.Document ? DocumentContentType : NewsContentType;
}
