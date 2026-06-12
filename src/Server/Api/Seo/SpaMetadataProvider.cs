namespace Api.Seo;

public class SpaMetadataProvider : ISpaMetadataProvider
{
    private const string HomepageJsonLd = """
        {
          "@context": "https://schema.org",
          "@graph": [
            {
              "@type": "Organization",
              "@id": "https://fibrasinmobiliarias.com/#organization",
              "name": "FIBRADIS",
              "url": "https://fibrasinmobiliarias.com",
              "logo": {
                "@type": "ImageObject",
                "url": "https://fibrasinmobiliarias.com/logo.png",
                "width": 512,
                "height": 512
              },
              "description": "Plataforma de análisis de FIBRAs inmobiliarias mexicanas: precios en tiempo real, distribuciones, fundamentales y ranking de oportunidades de inversión.",
              "areaServed": "MX",
              "foundingDate": "2023",
              "sameAs": [
                "https://twitter.com/fibradis",
                "https://linkedin.com/company/fibradis"
              ]
            },
            {
              "@type": "WebSite",
              "@id": "https://fibrasinmobiliarias.com/#website",
              "url": "https://fibrasinmobiliarias.com",
              "name": "FIBRADIS — Fibras Inmobiliarias",
              "publisher": { "@id": "https://fibrasinmobiliarias.com/#organization" },
              "inLanguage": "es-MX"
            },
            {
              "@type": "FinancialService",
              "@id": "https://fibrasinmobiliarias.com/#service",
              "name": "FIBRADIS — Análisis de FIBRAs Inmobiliarias",
              "url": "https://fibrasinmobiliarias.com",
              "provider": { "@id": "https://fibrasinmobiliarias.com/#organization" },
              "serviceType": "Análisis de inversiones inmobiliarias",
              "areaServed": {
                "@type": "Country",
                "name": "México"
              },
              "currenciesAccepted": "MXN",
              "audience": {
                "@type": "Audience",
                "audienceType": "Inversionistas inmobiliarios en México"
              }
            }
          ]
        }
        """;

    private const string ConoceLasFibrasJsonLd = """
        {
          "@context": "https://schema.org",
          "@type": "Article",
          "@id": "https://fibrasinmobiliarias.com/conoce-las-fibras#article",
          "headline": "¿Qué son las FIBRAs Inmobiliarias? Guía Completa",
          "description": "Aprende qué son las FIBRAs inmobiliarias, cómo funcionan, cómo invertir y qué beneficios fiscales ofrecen. Guía para inversionistas.",
          "url": "https://fibrasinmobiliarias.com/conoce-las-fibras",
          "inLanguage": "es-MX",
          "publisher": { "@id": "https://fibrasinmobiliarias.com/#organization" },
          "isPartOf": { "@id": "https://fibrasinmobiliarias.com/#website" }
        }
        """;

    private const string CalculadoraJsonLd = """
        {
          "@context": "https://schema.org",
          "@type": "SoftwareApplication",
          "@id": "https://fibrasinmobiliarias.com/calculadora#app",
          "name": "Calculadora ISR de FIBRAs",
          "url": "https://fibrasinmobiliarias.com/calculadora",
          "applicationCategory": "FinanceApplication",
          "operatingSystem": "Web",
          "offers": { "@type": "Offer", "price": "0", "priceCurrency": "MXN" },
          "provider": { "@id": "https://fibrasinmobiliarias.com/#organization" },
          "description": "Calcula el ISR de distribuciones de FIBRAs inmobiliarias mexicanas según la Ley del ISR vigente."
        }
        """;

    private static readonly IReadOnlyDictionary<string, SpaPageMeta> Routes =
        new Dictionary<string, SpaPageMeta>(StringComparer.Ordinal)
        {
            ["/"] = new(
                "FIBRAs Inmobiliarias — Análisis y Herramientas | FIBRADIS",
                "Plataforma de análisis de FIBRAs inmobiliarias mexicanas. Precios en tiempo real, distribuciones, fundamentales y ranking de oportunidades.",
                "/",
                HomepageJsonLd),
            ["/calculadora"] = new(
                "Calculadora ISR FIBRAs — Impuesto sobre la Renta | FIBRADIS",
                "Calcula el Impuesto Sobre la Renta (ISR) de tus distribuciones de FIBRAs inmobiliarias mexicanas. Herramienta gratuita con base en la Ley del ISR vigente.",
                "/calculadora",
                CalculadoraJsonLd),
            ["/comparar"] = new(
                "Comparar FIBRAs Inmobiliarias — Análisis Comparativo | FIBRADIS",
                "Compara hasta 4 FIBRAs inmobiliarias en precio, yield, fundamentales y score de oportunidad. Toma mejores decisiones de inversión.",
                "/comparar"),
            ["/fibras"] = new(
                "FIBRAs Inmobiliarias Mexicanas — Catálogo Completo | FIBRADIS",
                "Directorio completo de FIBRAs inmobiliarias en México con descripción, sector, precio y datos fundamentales de cada fideicomiso.",
                "/fibras"),
            ["/noticias"] = new(
                "Noticias FIBRAs Inmobiliarias | FIBRADIS",
                "Últimas noticias y novedades sobre el mercado de FIBRAs inmobiliarias mexicanas. Actualización continua desde fuentes especializadas.",
                "/noticias"),
            ["/conoce-las-fibras"] = new(
                "¿Qué son las FIBRAs Inmobiliarias? Guía Completa | FIBRADIS",
                "Aprende qué son las FIBRAs inmobiliarias, cómo funcionan, cómo invertir y qué beneficios fiscales ofrecen. Guía para inversionistas.",
                "/conoce-las-fibras",
                ConoceLasFibrasJsonLd),
            ["/calendario"] = new(
                "Calendario de Eventos Corporativos FIBRAs | FIBRADIS",
                "Próximas asambleas, distribuciones y eventos corporativos de FIBRAs inmobiliarias mexicanas. Mantente informado para tus decisiones.",
                "/calendario"),
            ["/fundamentales"] = new(
                "Fundamentales FIBRAs — Cap Rate, NAV, NOI | FIBRADIS",
                "Métricas fundamentales comparativas de FIBRAs: Cap Rate, NAV por CBFI, LTV, NOI Margin y más. Análisis cross-FIBRA actualizado.",
                "/fundamentales"),
            // agregada en code review 11-2 (deuda 10-2): ruta pública sin metadata server-side
            ["/herramientas"] = new(
                "Herramientas para Inversionistas en FIBRAs — Yield e ISR | FIBRADIS",
                "Calculadoras públicas para estimar yield anualizado e ISR de distribuciones de FIBRAs con cifras claras en MXN y resultados fáciles de leer.",
                "/herramientas"),
            ["/privacidad"] = new(
                "Aviso de Privacidad | FIBRADIS",
                "Aviso de privacidad de FIBRADIS: qué datos recopilamos, cómo los usamos, protección de datos y derechos de usuario conforme a la LFPDPPP.",
                "/privacidad"),
            ["/acerca"] = new(
                "Sobre FIBRADIS — Metodología y Fuentes de Datos | FIBRADIS",
                "Conoce la metodología de FIBRADIS: fuentes de datos, cálculo de fundamentales (Cap Rate, NAV, NOI) y scores de oportunidad para FIBRAs mexicanas.",
                "/acerca"),
            ["/contacto"] = new(
                "Contacto | FIBRADIS",
                "Contacta con FIBRADIS para reportar errores en datos, solicitar eliminación de cuenta o cualquier consulta sobre la plataforma.",
                "/contacto"),
        };

    public SpaPageMeta? GetMetaForPath(string path)
    {
        var normalizedPath = path.TrimEnd('/').ToLowerInvariant();
        if (normalizedPath.Length == 0)
            normalizedPath = "/";

        return Routes.TryGetValue(normalizedPath, out var meta) ? meta : null;
    }
}
