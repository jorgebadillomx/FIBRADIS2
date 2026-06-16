using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Application.Fundamentals;
using Application.Ops;
using Application.Seo;
using Domain.Fundamentals;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Seo;

public class SpaMetadataProvider(
    IConfiguration config,
    ISeoDefaultsBuilder seoDefaultsBuilder,
    IServiceScopeFactory scopeFactory) : ISpaMetadataProvider
{
    private const string BrandName = "Fibras Inmobiliarias";
    private const string DefaultDescription =
        "Plataforma de análisis de FIBRAs inmobiliarias mexicanas: precios en tiempo real, distribuciones, fundamentales y ranking de oportunidades de inversión.";
    private const string CalculadoraDescription =
        "Calcula cuántos CBFIs puedes comprar con tu presupuesto, qué distribución recibirías y tu renta bruta estimada para cada FIBRA inmobiliaria mexicana.";
    private const string ConoceDescription =
        "Aprende qué son las FIBRAs inmobiliarias, cómo funcionan, cómo invertir y qué beneficios fiscales ofrecen. Guía para inversionistas.";
    private const string CalendarioDescription =
        "Próximas asambleas, distribuciones y eventos corporativos de FIBRAs inmobiliarias mexicanas. Mantente informado para tus decisiones.";
    private const string FundamentalesDescription =
        "Métricas fundamentales comparativas de FIBRAs: Cap Rate, NAV por CBFI, LTV, NOI Margin y más. Análisis cross-FIBRA actualizado.";
    private const string PortafolioDescription =
        "Tu entrada pública a Fibras Inmobiliarias: explora portafolio, reportes, oportunidades, herramientas, fundamentales, noticias y catálogo, o inicia sesión.";
    private const string PlataformaTitle =
        "Plataforma de Fibras Inmobiliarias — descubre funciones y acceso | Fibras Inmobiliarias";
    private const string PlataformaDescription =
        "Descubre Fibras Inmobiliarias: catálogo, fichas, comparador, calculadora, noticias, calendario, portafolio, reportes y herramientas para invertir mejor.";
    private const string PrivacyDescription =
        "Aviso de privacidad de Fibras Inmobiliarias: qué datos recopilamos, cómo los usamos, protección de datos y derechos de usuario conforme a la LFPDPPP.";
    private const string AboutDescription =
        "Conoce la metodología de Fibras Inmobiliarias: fuentes de datos, cálculo de fundamentales (Cap Rate, NAV, NOI) y scores de oportunidad para FIBRAs mexicanas.";
    private const string ContactDescription =
        "Contacta con Fibras Inmobiliarias para reportar errores en datos, solicitar eliminación de cuenta o cualquier consulta sobre la plataforma.";

    private static readonly JsonSerializerOptions JsonLdOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    private readonly string _baseUrl = !string.IsNullOrWhiteSpace(config["App:BaseUrl"])
        ? config["App:BaseUrl"]!.TrimEnd('/')
        : throw new InvalidOperationException(
            "App:BaseUrl es requerido por SpaMetadataProvider para construir canonical/og:url absolutos.");

    // Fuente de verdad de las rutas fijas para el seed/backfill. Debe reflejar exactamente los
    // casos del switch de GetMetaForPathAsync (excluye /herramientas, ruta privada tras 11-6).
    public IReadOnlyList<string> KnownPaths { get; } =
    [
        "/", "/calculadora", "/comparar", "/fibras", "/noticias", "/conoce-las-fibras",
        "/calendario", "/fundamentales", "/plataforma", "/portafolio", "/privacidad", "/acerca", "/contacto",
    ];

    public async Task<SpaPageMeta?> GetMetaForPathAsync(string path, CancellationToken ct = default)
    {
        var normalizedPath = NormalizePath(path);

        return normalizedPath switch
        {
            "/" => new SpaPageMeta(
                "FIBRAs Inmobiliarias — Análisis y Herramientas | Fibras Inmobiliarias",
                DefaultDescription,
                "/",
                await BuildHomepageJsonLdAsync(ct)),
            "/calculadora" => new SpaPageMeta(
                "Calculadora de FIBRAs — ¿Cuántos CBFIs puedo comprar? | Fibras Inmobiliarias",
                CalculadoraDescription,
                "/calculadora",
                BuildCalculadoraJsonLd()),
            "/comparar" => new SpaPageMeta(
                "Comparar FIBRAs Inmobiliarias — Análisis Comparativo | Fibras Inmobiliarias",
                "Compara hasta 4 FIBRAs inmobiliarias en precio, yield, fundamentales y score de oportunidad. Toma mejores decisiones de inversión.",
                "/comparar",
                BuildCompareJsonLd()),
            "/fibras" => new SpaPageMeta(
                "FIBRAs Inmobiliarias Mexicanas — Catálogo Completo | Fibras Inmobiliarias",
                "Directorio completo de FIBRAs inmobiliarias en México con descripción, sector, precio y datos fundamentales de cada fideicomiso.",
                "/fibras"),
            "/noticias" => new SpaPageMeta(
                "Noticias FIBRAs Inmobiliarias | Fibras Inmobiliarias",
                "Últimas noticias y novedades sobre el mercado de FIBRAs inmobiliarias mexicanas. Actualización continua desde fuentes especializadas.",
                "/noticias"),
            "/conoce-las-fibras" => new SpaPageMeta(
                "¿Qué son las FIBRAs Inmobiliarias? Guía Completa | Fibras Inmobiliarias",
                ConoceDescription,
                "/conoce-las-fibras",
                await BuildConoceLasFibrasJsonLdAsync(ct)),
            "/calendario" => new SpaPageMeta(
                "Calendario de Eventos Corporativos FIBRAs | Fibras Inmobiliarias",
                CalendarioDescription,
                "/calendario"),
            "/fundamentales" => new SpaPageMeta(
                "Fundamentales FIBRAs — Cap Rate, NAV, NOI | Fibras Inmobiliarias",
                FundamentalesDescription,
                "/fundamentales",
                await BuildFundamentalesJsonLdAsync(ct)),
            "/plataforma" => new SpaPageMeta(
                PlataformaTitle,
                PlataformaDescription,
                "/plataforma",
                BuildPlataformaJsonLd()),
            "/portafolio" => new SpaPageMeta(
                "Portafolio de FIBRAs, reportes y login | Fibras Inmobiliarias",
                PortafolioDescription,
                "/portafolio",
                BuildPortafolioJsonLd()),
            "/privacidad" => new SpaPageMeta(
                "Aviso de Privacidad | Fibras Inmobiliarias",
                PrivacyDescription,
                "/privacidad"),
            "/acerca" => new SpaPageMeta(
                "Sobre Fibras Inmobiliarias — Metodología y Fuentes de Datos | Fibras Inmobiliarias",
                AboutDescription,
                "/acerca",
                await BuildAboutJsonLdAsync(ct)),
            "/contacto" => new SpaPageMeta(
                "Contacto | Fibras Inmobiliarias",
                ContactDescription,
                "/contacto",
                await BuildContactJsonLdAsync(ct)),
            _ => null,
        };
    }

    private string BuildCalculadoraJsonLd()
        => JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "SoftwareApplication",
            ["@id"] = $"{_baseUrl}/calculadora#app",
            ["name"] = "Calculadora de compra de FIBRAs",
            ["url"] = $"{_baseUrl}/calculadora",
            ["applicationCategory"] = "FinanceApplication",
            ["operatingSystem"] = "Web",
            ["offers"] = new Dictionary<string, object?>
            {
                ["@type"] = "Offer",
                ["price"] = "0",
                ["priceCurrency"] = "MXN",
            },
            ["provider"] = new Dictionary<string, object?>
            {
                ["@id"] = $"{_baseUrl}/#organization",
            },
            ["description"] = CalculadoraDescription,
        }, JsonLdOptions);

    private string BuildCompareJsonLd()
        => seoDefaultsBuilder.BuildComparePageJsonLd(Array.Empty<(string FullName, string Ticker)>(), _baseUrl);

    private async Task<string> BuildHomepageJsonLdAsync(CancellationToken ct)
    {
        // La home necesita email + sameAs de la misma fila OperationalConfig:
        // una sola lectura (la home es ruta caliente y se sirve no-cache).
        var (contactEmail, sameAs) = await GetOrganizationContactDataAsync(ct);

        var organization = new Dictionary<string, object?>
        {
            ["@type"] = "Organization",
            ["@id"] = $"{_baseUrl}/#organization",
            ["name"] = BrandName,
            ["url"] = _baseUrl,
            ["logo"] = new Dictionary<string, object?>
            {
                ["@type"] = "ImageObject",
                ["url"] = $"{_baseUrl}/logo.png",
                ["width"] = 512,
                ["height"] = 512,
            },
            ["description"] = DefaultDescription,
            ["areaServed"] = "MX",
            ["foundingDate"] = "2023",
            ["knowsAbout"] = new[] { "FIBRAs", "REITs México", "inversión inmobiliaria" },
        };

        if (!string.IsNullOrWhiteSpace(contactEmail))
            organization["email"] = contactEmail;

        if (sameAs.Count > 0)
            organization["sameAs"] = sameAs;

        var json = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = new object[]
            {
                organization,
                new Dictionary<string, object?>
                {
                    ["@type"] = "WebSite",
                    ["@id"] = $"{_baseUrl}/#website",
                    ["url"] = _baseUrl,
                    ["name"] = BrandName,
                    ["publisher"] = new Dictionary<string, object?>
                    {
                        ["@id"] = $"{_baseUrl}/#organization",
                    },
                    ["inLanguage"] = "es-MX",
                },
                new Dictionary<string, object?>
                {
                    ["@type"] = "FinancialService",
                    ["@id"] = $"{_baseUrl}/#service",
                    ["name"] = BrandName,
                    ["url"] = _baseUrl,
                    ["provider"] = new Dictionary<string, object?>
                    {
                        ["@id"] = $"{_baseUrl}/#organization",
                    },
                    ["serviceType"] = "Análisis de inversiones inmobiliarias",
                    ["areaServed"] = new Dictionary<string, object?>
                    {
                        ["@type"] = "Country",
                        ["name"] = "México",
                    },
                    ["currenciesAccepted"] = "MXN",
                    ["audience"] = new Dictionary<string, object?>
                    {
                        ["@type"] = "Audience",
                        ["audienceType"] = "Inversionistas inmobiliarios en México",
                    },
                },
            },
        };

        return JsonSerializer.Serialize(json, JsonLdOptions);
    }

    private async Task<string> BuildConoceLasFibrasJsonLdAsync(CancellationToken ct)
    {
        var updatedAt = await GetLatestEditorialUpdateAsync(ct);
        var json = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Article",
            ["@id"] = $"{_baseUrl}/conoce-las-fibras#article",
            ["headline"] = "¿Qué son las FIBRAs Inmobiliarias? Guía Completa",
            ["description"] = ConoceDescription,
            ["url"] = $"{_baseUrl}/conoce-las-fibras",
            ["inLanguage"] = "es-MX",
            ["author"] = new Dictionary<string, object?>
            {
                ["@type"] = "Organization",
                ["name"] = BrandName,
                ["url"] = _baseUrl,
            },
            ["publisher"] = new Dictionary<string, object?>
            {
                ["@id"] = $"{_baseUrl}/#organization",
            },
            ["isPartOf"] = new Dictionary<string, object?>
            {
                ["@id"] = $"{_baseUrl}/#website",
            },
        };

        if (updatedAt is not null)
            json["dateModified"] = updatedAt.Value.ToString("o");

        return JsonSerializer.Serialize(json, JsonLdOptions);
    }

    private Task<string> BuildFundamentalesJsonLdAsync(CancellationToken ct)
        => Task.FromResult(seoDefaultsBuilder.BuildFundamentalesPageJsonLd(
            Array.Empty<(FundamentalRecord Record, string Ticker, string ShortName)>(),
            _baseUrl));

    private string BuildPortafolioJsonLd()
        => JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["@type"] = "CollectionPage",
                    ["@id"] = $"{_baseUrl}/portafolio#page",
                    ["name"] = "Portafolio de FIBRAs, reportes y login | Fibras Inmobiliarias",
                    ["description"] = PortafolioDescription,
                    ["url"] = $"{_baseUrl}/portafolio",
                    ["isPartOf"] = new Dictionary<string, object?>
                    {
                        ["@id"] = $"{_baseUrl}/#website",
                    },
                    ["publisher"] = new Dictionary<string, object?>
                    {
                        ["@id"] = $"{_baseUrl}/#organization",
                    },
                    ["mainEntity"] = new Dictionary<string, object?>
                    {
                        ["@type"] = "ItemList",
                        ["name"] = "Capacidades de Fibras Inmobiliarias",
                        ["numberOfItems"] = 7,
                        ["itemListOrder"] = "https://schema.org/ItemListOrderAscending",
                        ["itemListElement"] = new object[]
                        {
                            new Dictionary<string, object?>
                            {
                                ["@type"] = "ListItem",
                                ["position"] = 1,
                                ["name"] = "Portafolio",
                                ["url"] = $"{_baseUrl}/portafolio",
                            },
                            new Dictionary<string, object?>
                            {
                                ["@type"] = "ListItem",
                                ["position"] = 2,
                                ["name"] = "Reportes trimestrales",
                                ["url"] = $"{_baseUrl}/reportes",
                            },
                            new Dictionary<string, object?>
                            {
                                ["@type"] = "ListItem",
                                ["position"] = 3,
                                ["name"] = "Oportunidades y ranking",
                                ["url"] = $"{_baseUrl}/oportunidades",
                            },
                            new Dictionary<string, object?>
                            {
                                ["@type"] = "ListItem",
                                ["position"] = 4,
                                ["name"] = "Herramientas y calculadora",
                                ["url"] = $"{_baseUrl}/calculadora",
                            },
                            new Dictionary<string, object?>
                            {
                                ["@type"] = "ListItem",
                                ["position"] = 5,
                                ["name"] = "Fundamentales comparativos",
                                ["url"] = $"{_baseUrl}/fundamentales",
                            },
                            new Dictionary<string, object?>
                            {
                                ["@type"] = "ListItem",
                                ["position"] = 6,
                                ["name"] = "Noticias",
                                ["url"] = $"{_baseUrl}/noticias",
                            },
                            new Dictionary<string, object?>
                            {
                                ["@type"] = "ListItem",
                                ["position"] = 7,
                                ["name"] = "Catálogo de FIBRAs",
                                ["url"] = $"{_baseUrl}/fibras",
                            },
                        },
                    },
                },
            },
        }, JsonLdOptions);

    private string BuildPlataformaJsonLd()
        => JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["@type"] = "CollectionPage",
                    ["@id"] = $"{_baseUrl}/plataforma#page",
                    ["name"] = PlataformaTitle,
                    ["description"] = PlataformaDescription,
                    ["url"] = $"{_baseUrl}/plataforma",
                    ["isPartOf"] = new Dictionary<string, object?>
                    {
                        ["@id"] = $"{_baseUrl}/#website",
                    },
                    ["publisher"] = new Dictionary<string, object?>
                    {
                        ["@id"] = $"{_baseUrl}/#organization",
                    },
                    ["mainEntity"] = new Dictionary<string, object?>
                    {
                        ["@type"] = "ItemList",
                        ["name"] = "Funciones públicas de Fibras Inmobiliarias",
                        ["numberOfItems"] = 7,
                        ["itemListOrder"] = "https://schema.org/ItemListOrderAscending",
                        ["itemListElement"] = new object[]
                        {
                            new Dictionary<string, object?>
                            {
                                ["@type"] = "ListItem",
                                ["position"] = 1,
                                ["name"] = "Catálogo y fichas de FIBRAs",
                                ["url"] = $"{_baseUrl}/fibras",
                            },
                            new Dictionary<string, object?>
                            {
                                ["@type"] = "ListItem",
                                ["position"] = 2,
                                ["name"] = "Comparador",
                                ["url"] = $"{_baseUrl}/comparar",
                            },
                            new Dictionary<string, object?>
                            {
                                ["@type"] = "ListItem",
                                ["position"] = 3,
                                ["name"] = "Fundamentales",
                                ["url"] = $"{_baseUrl}/fundamentales",
                            },
                            new Dictionary<string, object?>
                            {
                                ["@type"] = "ListItem",
                                ["position"] = 4,
                                ["name"] = "Calculadora",
                                ["url"] = $"{_baseUrl}/calculadora",
                            },
                            new Dictionary<string, object?>
                            {
                                ["@type"] = "ListItem",
                                ["position"] = 5,
                                ["name"] = "Calendario",
                                ["url"] = $"{_baseUrl}/calendario",
                            },
                            new Dictionary<string, object?>
                            {
                                ["@type"] = "ListItem",
                                ["position"] = 6,
                                ["name"] = "Noticias",
                                ["url"] = $"{_baseUrl}/noticias",
                            },
                            new Dictionary<string, object?>
                            {
                                ["@type"] = "ListItem",
                                ["position"] = 7,
                                ["name"] = "Guía ¿Qué son las FIBRAs?",
                                ["url"] = $"{_baseUrl}/conoce-las-fibras",
                            },
                        },
                    },
                },
            },
        }, JsonLdOptions);

    private async Task<string> BuildAboutJsonLdAsync(CancellationToken ct)
    {
        var contactEmail = await GetContactEmailAsync(ct);
        var json = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "AboutPage",
            ["@id"] = $"{_baseUrl}/acerca#page",
            ["name"] = "Sobre Fibras Inmobiliarias — Metodología y Fuentes de Datos | Fibras Inmobiliarias",
            ["description"] = AboutDescription,
            ["url"] = $"{_baseUrl}/acerca",
            ["publisher"] = new Dictionary<string, object?>
            {
                ["@id"] = $"{_baseUrl}/#organization",
            },
            ["about"] = new Dictionary<string, object?>
            {
                ["@id"] = $"{_baseUrl}/#organization",
            },
        };

        if (!string.IsNullOrWhiteSpace(contactEmail))
        {
            json["potentialAction"] = new Dictionary<string, object?>
            {
                ["@type"] = "ContactAction",
                ["target"] = new Dictionary<string, object?>
                {
                    ["@type"] = "EntryPoint",
                    ["urlTemplate"] = $"mailto:{contactEmail}",
                },
            };
        }

        return JsonSerializer.Serialize(json, JsonLdOptions);
    }

    private async Task<string> BuildContactJsonLdAsync(CancellationToken ct)
    {
        var contactEmail = await GetContactEmailAsync(ct);
        var contactPoint = new Dictionary<string, object?>
        {
            ["@type"] = "ContactPoint",
            ["contactType"] = "customer support",
            ["availableLanguage"] = new[] { "es" },
            ["areaServed"] = "MX",
        };

        if (!string.IsNullOrWhiteSpace(contactEmail))
            contactPoint["email"] = contactEmail;

        var json = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "ContactPage",
            ["@id"] = $"{_baseUrl}/contacto#page",
            ["name"] = "Contacto | Fibras Inmobiliarias",
            ["description"] = ContactDescription,
            ["url"] = $"{_baseUrl}/contacto",
            ["mainEntity"] = new Dictionary<string, object?>
            {
                ["@type"] = "Organization",
                ["name"] = BrandName,
                ["url"] = _baseUrl,
                ["contactPoint"] = contactPoint,
            },
        };

        return JsonSerializer.Serialize(json, JsonLdOptions);
    }

    private async Task<string?> GetContactEmailAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOperationalConfigRepository>();
        var configRow = await repo.GetAsync(ct);
        return string.IsNullOrWhiteSpace(configRow.ContactEmail) ? null : configRow.ContactEmail.Trim();
    }

    private async Task<(string? ContactEmail, IReadOnlyList<string> SameAs)> GetOrganizationContactDataAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IOperationalConfigRepository>();
        var configRow = await repo.GetAsync(ct);
        var email = string.IsNullOrWhiteSpace(configRow.ContactEmail) ? null : configRow.ContactEmail.Trim();
        return (email, ParseSameAs(configRow.OrganizationSameAsJson));
    }

    private static IReadOnlyList<string> ParseSameAs(string? sameAsJson)
    {
        if (string.IsNullOrWhiteSpace(sameAsJson))
            return Array.Empty<string>();

        try
        {
            var urls = JsonSerializer.Deserialize<string?[]>(sameAsJson);
            if (urls is null || urls.Length == 0)
                return Array.Empty<string>();

            return urls
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Select(url => url!.Trim())
                .Where(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                    && (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                        || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    private async Task<DateTimeOffset?> GetLatestEditorialUpdateAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IEditorialPageRepository>();
        var pages = await repo.GetAllAsync(ct);
        return pages.Count == 0 ? null : pages.Max(page => page.UpdatedAt);
    }

    private static string NormalizePath(string path)
    {
        var normalized = path.TrimEnd('/').ToLowerInvariant();
        return normalized.Length == 0 ? "/" : normalized;
    }
}
