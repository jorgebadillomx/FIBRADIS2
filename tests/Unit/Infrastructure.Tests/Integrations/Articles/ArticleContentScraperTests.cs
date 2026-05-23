using System.Net;
using Application.News;
using Infrastructure.Integrations.Articles;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Integrations.Articles;

public class ArticleContentScraperTests
{
    // ── existing: Google News URL decoding ─────────────────────────────────────

    [Fact]
    public async Task TryGetArticleTextAsync_WhenUrlIsGoogleNews_UsesDecodedPublisherUrl()
    {
        var decoder = new StubGoogleNewsUrlDecoder("https://www.eleconomista.com.mx/opinion/fibra-danhos-pagara-dividendo-20260224-801518.html");
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal("https://www.eleconomista.com.mx/opinion/fibra-danhos-pagara-dividendo-20260224-801518.html", request.RequestUri!.AbsoluteUri);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    <html>
                      <body>
                        <article>
                          <h1>Fibra Danhos pagará dividendo</h1>
                          <p>Fibra Danhos anunció el pago de un dividendo trimestral a sus tenedores de CBFIs.</p>
                          <p>La emisora reiteró señales de estabilidad operativa y generación de flujo libre consistente.</p>
                          <p>El management confirmó que la distribución refleja el desempeño sólido del portafolio de centros comerciales premium.</p>
                        </article>
                      </body>
                    </html>
                    """),
            });
        });

        var scraper = new ArticleContentScraper(
            new HttpClient(handler),
            decoder,
            NullLogger<ArticleContentScraper>.Instance);

        var body = await scraper.TryGetArticleTextAsync("https://news.google.com/rss/articles/demo?oc=5");

        Assert.NotNull(body);
        Assert.Contains("Fibra Danhos anunció el pago de un dividendo", body);
        Assert.Contains("estabilidad operativa", body);
    }

    // ── AC 1 + AC 2: extracción semántica + eliminación de boilerplate ─────────

    [Fact]
    public async Task TryGetArticleTextAsync_WithArticleTag_ExtractsOnlyArticleContent()
    {
        var handler = HtmlHandler("""
            <html>
              <header>
                <nav>Inicio | FIBRAs | Mercados | Suscríbete | Login</nav>
              </header>
              <article>
                <h1>Fibra Danhos reporta crecimiento en NOI</h1>
                <p>Fibra Danhos reportó un crecimiento del 8% en su NOI trimestral, superando las expectativas del mercado.</p>
                <p>El management destacó la resiliencia de sus activos comerciales premium en las principales plazas del país.</p>
                <p>La empresa mantiene una ocupación del 94% en su portafolio diversificado de centros comerciales de primer nivel.</p>
              </article>
              <footer>
                <p>Síguenos en redes sociales. Suscríbete al newsletter. Copyright 2026.</p>
              </footer>
            </html>
            """);

        var body = await MakeScraper(handler).TryGetArticleTextAsync("https://example.com/noticia");

        Assert.NotNull(body);
        Assert.Contains("crecimiento del 8%", body);
        Assert.Contains("resiliencia de sus activos", body);
        Assert.DoesNotContain("Suscríbete", body);
        Assert.DoesNotContain("Síguenos en redes", body);
        Assert.DoesNotContain("Login", body);
    }

    [Fact]
    public async Task TryGetArticleTextAsync_WithItempropArticleBody_ExtractsMainContent()
    {
        var handler = HtmlHandler("""
            <html>
              <header><nav>Nav boilerplate | Leer más | Login</nav></header>
              <div class="layout">
                <div itemprop="articleBody">
                  <p>Fibra Monterrey anunció la adquisición de tres inmuebles industriales en el Bajío por 180 millones de dólares.</p>
                  <p>Los activos cuentan con contratos de arrendamiento a largo plazo con inquilinos internacionales de manufactura avanzada.</p>
                  <p>La operación eleva el portafolio total de la FIBRA a 4.2 millones de metros cuadrados rentables.</p>
                </div>
                <aside>Últimas noticias | Más leídas | Publicidad</aside>
              </div>
              <footer>Footer boilerplate</footer>
            </html>
            """);

        var body = await MakeScraper(handler).TryGetArticleTextAsync("https://example.com/noticia");

        Assert.NotNull(body);
        Assert.Contains("adquisición de tres inmuebles industriales", body);
        Assert.Contains("contratos de arrendamiento", body);
        Assert.DoesNotContain("Últimas noticias", body);
        Assert.DoesNotContain("Publicidad", body);
    }

    [Fact]
    public async Task TryGetArticleTextAsync_WithMainTag_ExtractsMainContent()
    {
        var handler = HtmlHandler("""
            <html>
              <nav>Menu global | Suscripción | Alertas</nav>
              <main>
                <h1>Fibra Uno reporta ocupación récord</h1>
                <p>Fibra Uno alcanzó una ocupación récord del 96% en su portafolio comercial e industrial durante el trimestre.</p>
                <p>La directora de relaciones con inversionistas destacó la solidez del mercado de renta y la demanda institucional sostenida.</p>
                <p>El consenso de analistas mantiene una recomendación de compra con precio objetivo de 35 pesos por CBFI.</p>
              </main>
              <footer>Footer | Aviso de privacidad | Cookies</footer>
            </html>
            """);

        var body = await MakeScraper(handler).TryGetArticleTextAsync("https://example.com/noticia");

        Assert.NotNull(body);
        Assert.Contains("ocupación récord del 96%", body);
        Assert.Contains("relaciones con inversionistas", body);
        Assert.DoesNotContain("Cookies", body);
        Assert.DoesNotContain("Aviso de privacidad", body);
    }

    // ── Phase 2: CMS content-class patterns ───────────────────────────────────

    [Fact]
    public async Task TryGetArticleTextAsync_WithArticleBodyClass_ExtractsContentAndIgnoresNav()
    {
        var handler = HtmlHandler("""
            <html>
              <nav>Inicio | FIBRAs | Mercados | Suscríbete</nav>
              <div id="wrapper">
                <div class="article-body">
                  <p>Fibra MQ reportó un incremento del 15% en sus ingresos operativos netos del trimestre, superando las expectativas del consenso de analistas del sector inmobiliario.</p>
                  <p>El portafolio industrial de la FIBRA alcanzó una ocupación del 98% impulsado por la expansión del sector manufacturero en el Bajío y norte del país.</p>
                  <p>La directora de relaciones con inversionistas destacó que los contratos de arrendamiento de largo plazo sustentan la visibilidad de flujos futuros.</p>
                </div>
                <div class="sidebar">Más leídas | Relacionadas | Publicidad</div>
              </div>
              <footer>Footer del sitio | Aviso de privacidad</footer>
            </html>
            """);

        var body = await MakeScraper(handler).TryGetArticleTextAsync("https://example.com/noticia");

        Assert.NotNull(body);
        Assert.Contains("incremento del 15%", body);
        Assert.Contains("contratos de arrendamiento", body);
        Assert.DoesNotContain("Suscríbete", body);
    }

    [Fact]
    public async Task TryGetArticleTextAsync_WithEntryContentClass_ExtractsWordPressStyle()
    {
        var handler = HtmlHandler("""
            <html>
              <header><nav>Menú principal | Categorías | Login</nav></header>
              <section class="entry-content">
                <p>Fibra Danhos anunció la apertura de su nuevo proyecto Plaza Carso II con una inversión de 3,500 millones de pesos en la Ciudad de México.</p>
                <p>El desarrollo de uso mixto contará con 80,000 metros cuadrados de área rentable comercial y de oficinas premium de clase A.</p>
                <p>La ocupación proyectada al primer año supera el 90% con anclas ya firmadas de retail internacional y corporativos nacionales.</p>
              </section>
              <footer>Footer | Compartir | Suscríbete</footer>
            </html>
            """);

        var body = await MakeScraper(handler).TryGetArticleTextAsync("https://example.com/noticia");

        Assert.NotNull(body);
        Assert.Contains("Plaza Carso II", body);
        Assert.Contains("área rentable comercial", body);
        Assert.DoesNotContain("Login", body);
    }

    [Fact]
    public async Task TryGetArticleTextAsync_ParagraphsWithNavKeywords_FiltersThemOut()
    {
        var handler = HtmlHandler("""
            <html>
              <div id="content">
                <p>Buscar noticias sobre FIBRAs en nuestro portal</p>
                <p>Suscríbete para recibir alertas del mercado inmobiliario</p>
                <p>Fibra Uno publicó sus resultados del primer trimestre con un crecimiento del 9% en NOI comparable ajustado por inflación.</p>
                <p>La cartera de propiedades industriales y comerciales alcanzó una valuación total de 145 mil millones de pesos al cierre del período.</p>
                <p>Ver más noticias relacionadas con el sector</p>
                <p>Los analistas de Banorte mantienen su recomendación de compra con precio objetivo de 38 pesos por CBFI para los próximos doce meses.</p>
              </div>
            </html>
            """);

        var body = await MakeScraper(handler).TryGetArticleTextAsync("https://example.com/noticia");

        Assert.NotNull(body);
        Assert.Contains("crecimiento del 9%", body);
        Assert.Contains("recomendación de compra", body);
        Assert.DoesNotContain("Suscríbete", body);
        Assert.DoesNotContain("Buscar noticias", body);
        Assert.DoesNotContain("Ver más noticias", body);
    }

    [Fact]
    public async Task TryGetArticleTextAsync_ParagraphsWithPipeSeparators_FiltersNavBars()
    {
        var handler = HtmlHandler("""
            <html>
              <div id="page">
                <p>Inicio | Noticias | FIBRAs | Mercado | Contacto | Suscripción</p>
                <p>Economía | Política | Mercados | Internacional | Tecnología | Finanzas</p>
                <p>Fibra Macquarie anunció la distribución de 0.42 pesos por CBFI correspondiente al cuarto trimestre del ejercicio fiscal.</p>
                <p>El monto representa un incremento del 5% respecto al trimestre anterior y refleja la solidez operativa del portafolio de parques industriales.</p>
                <p>La distribución será pagada el próximo 15 de enero a los tenedores registrados al cierre del 31 de diciembre.</p>
              </div>
            </html>
            """);

        var body = await MakeScraper(handler).TryGetArticleTextAsync("https://example.com/noticia");

        Assert.NotNull(body);
        Assert.Contains("distribución de 0.42 pesos", body);
        Assert.Contains("incremento del 5%", body);
        Assert.DoesNotContain("Inicio | Noticias", body);
        Assert.DoesNotContain("Economía | Política", body);
    }

    // ── AC 3: fallback heurístico ──────────────────────────────────────────────

    [Fact]
    public async Task TryGetArticleTextAsync_WithNoSemanticTags_FallsBackToParagraphExtraction()
    {
        var handler = HtmlHandler("""
            <html>
              <nav>Inicio | FIBRAs | Mercados | Suscríbete | Login</nav>
              <div id="main-content">
                <p>Fibra MQ reportó un incremento del 12% en sus ingresos operativos durante el primer trimestre del año fiscal.</p>
                <p>El portafolio industrial de la FIBRA alcanzó una ocupación récord del 98% impulsado por la demanda del sector manufacturero.</p>
                <p>Los analistas destacaron la solidez del balance y la estrategia de crecimiento en el Bajío y norte del país.</p>
              </div>
              <footer>Suscríbete | Política de privacidad | Síguenos en Twitter</footer>
            </html>
            """);

        var body = await MakeScraper(handler).TryGetArticleTextAsync("https://example.com/noticia");

        Assert.NotNull(body);
        Assert.Contains("incremento del 12%", body);
        Assert.Contains("portafolio industrial", body);
        Assert.DoesNotContain("Suscríbete", body);
        Assert.DoesNotContain("Política de privacidad", body);
    }

    // ── AC 5: null preferible a basura ─────────────────────────────────────────

    [Fact]
    public async Task TryGetArticleTextAsync_WhenContentTooShort_ReturnsNull()
    {
        var handler = HtmlHandler("""
            <html>
              <header><nav>Menú</nav></header>
              <div>Ok</div>
              <footer>Footer</footer>
            </html>
            """);

        var body = await MakeScraper(handler).TryGetArticleTextAsync("https://example.com/noticia");

        Assert.Null(body);
    }

    [Fact]
    public async Task TryGetArticleTextAsync_WhenOnlyBoilerplateAndShortParagraphs_ReturnsNull()
    {
        var handler = HtmlHandler("""
            <html>
              <nav>Inicio | FIBRAs | Login | Suscríbete</nav>
              <header>Logo del sitio</header>
              <div>
                <p>Ver más</p>
                <p>Leer también</p>
                <p>Compartir</p>
              </div>
              <footer>Copyright 2026</footer>
            </html>
            """);

        var body = await MakeScraper(handler).TryGetArticleTextAsync("https://example.com/noticia");

        Assert.Null(body);
    }

    [Fact]
    public async Task TryGetArticleTextAsync_WhenExtractedTextIsOnlyGoogleNews_ReturnsNull()
    {
        var handler = HtmlHandler("""
            <html>
              <body>
                <article>Google News</article>
              </body>
            </html>
            """);

        var body = await MakeScraper(handler).TryGetArticleTextAsync("https://example.com/noticia");

        Assert.Null(body);
    }

    // ── AC 2: bug de article anidado resuelto ─────────────────────────────────

    [Fact]
    public async Task TryGetArticleTextAsync_WithNestedArticle_ExtractsOuterArticleNotInner()
    {
        var handler = HtmlHandler("""
            <html>
              <body>
                <article>
                  <h1>Fibra Uno aumenta distribución trimestral</h1>
                  <p>Fibra Uno anunció un incremento del 7% en su distribución trimestral por CBFI, superando las expectativas del consenso de analistas del sector inmobiliario mexicano.</p>
                  <p>El NOI comparable del portafolio industrial y comercial creció 9.3% en términos reales, reflejo de la alta ocupación y la indexación de contratos a inflación.</p>
                  <p>El management confirmó que la estrategia de reciclaje de capital permitirá mantener el crecimiento de distribuciones en los próximos cuatro trimestres.</p>
                  <div class="related">
                    <article>Artículo relacionado: FMTY también aumenta su distribución este trimestre.</article>
                  </div>
                </article>
              </body>
            </html>
            """);

        var body = await MakeScraper(handler).TryGetArticleTextAsync("https://example.com/noticia");

        Assert.NotNull(body);
        Assert.Contains("incremento del 7%", body);
        Assert.Contains("reciclaje de capital", body);
        Assert.DoesNotContain("FMTY también aumenta", body);
        Assert.DoesNotContain("Artículo relacionado", body);
    }

    // ── AC 3: catálogo tiene prioridad sobre selector genérico ─────────────────

    [Fact]
    public async Task TryGetArticleTextAsync_WithCatalogMatch_UsesCatalogSelectorFirst()
    {
        // El HTML tiene <article> con texto genérico Y .nota-body con el contenido editorial real.
        // El catálogo de elfinanciero.com.mx debe priorizar .nota-body sobre <article>.
        var handler = HtmlHandler("""
            <html>
              <body>
                <article>
                  <p>Este texto genérico del article no debe aparecer en el body_text extraído.</p>
                  <p>Segundo párrafo genérico que tampoco debe capturarse si el catálogo funciona.</p>
                </article>
                <div class="nota-body">
                  <p>Fibra Danhos reportó resultados del tercer trimestre con un incremento del 11% en el NOI de su portafolio de centros comerciales premium en la Ciudad de México.</p>
                  <p>La ocupación del portafolio alcanzó el 93.5% al cierre del trimestre, impulsada por la apertura de nuevas marcas internacionales en sus plazas insignia.</p>
                  <p>El CFO destacó que los contratos de arrendamiento a largo plazo con cláusulas de ajuste anual aseguran visibilidad de flujos para los próximos cinco años.</p>
                </div>
              </body>
            </html>
            """);

        // Usar hostname de elfinanciero.com.mx — presente en SiteExtractionCatalog
        var body = await MakeScraper(handler).TryGetArticleTextAsync("https://www.elfinanciero.com.mx/mercados/2026/fibra-danhos-resultados");

        Assert.NotNull(body);
        Assert.Contains("incremento del 11%", body);
        Assert.Contains("contratos de arrendamiento a largo plazo", body);
        Assert.DoesNotContain("texto genérico del article", body);
        Assert.DoesNotContain("Segundo párrafo genérico", body);
    }

    // ── Phase 5: og:description / meta description fallback ──────────────────

    [Fact]
    public async Task TryGetArticleTextAsync_WhenSpaWithOgDescription_ReturnsMetaDescription()
    {
        // SPA que no tiene body_text procesable pero sí og:description (ej. Vivanuncios)
        var handler = HtmlHandler("""
            <html>
              <head>
                <meta property="og:description" content="Fibra Mty reportó un incremento de 12% en sus ingresos operativos durante el primer trimestre, impulsado por la alta demanda de espacios industriales en la zona noreste del país." />
              </head>
              <body>
                <script>window.__INITIAL_STATE__ = {};</script>
                <div class="spa-root"></div>
              </body>
            </html>
            """);

        var body = await MakeScraper(handler).TryGetArticleTextAsync("https://example.com/noticias/noticia");

        Assert.NotNull(body);
        Assert.Contains("Fibra Mty reportó un incremento de 12%", body);
    }

    [Fact]
    public async Task TryGetArticleTextAsync_WhenArticleBodyExistsOgDescriptionIsIgnored()
    {
        // Phases 1-4 deben ganar; og:description no debe usarse cuando ya hay contenido
        var handler = HtmlHandler("""
            <html>
              <head>
                <meta property="og:description" content="Resumen corto del artículo en og:description que NO debe aparecer." />
              </head>
              <body>
                <article>
                  <p>Fibra Uno anunció la adquisición de un portafolio industrial por 200 millones de dólares en el corredor Bajío, ampliando su presencia en nearshoring.</p>
                  <p>La operación eleva el total de metros cuadrados arrendables del fideicomiso a 9.5 millones, con una tasa de ocupación del 97.8% al cierre del trimestre.</p>
                  <p>Los analistas de Banorte elevaron su precio objetivo a 35 pesos por CBFI ante la visibilidad de flujos que brinda el nuevo portafolio.</p>
                </article>
              </body>
            </html>
            """);

        var body = await MakeScraper(handler).TryGetArticleTextAsync("https://example.com/noticias/noticia");

        Assert.NotNull(body);
        Assert.Contains("portafolio industrial por 200 millones", body);
        Assert.DoesNotContain("Resumen corto del artículo en og:description", body);
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private static ArticleContentScraper MakeScraper(StubHttpMessageHandler handler)
        => new(new HttpClient(handler), new PassthroughDecoder(), NullLogger<ArticleContentScraper>.Instance);

    private static StubHttpMessageHandler HtmlHandler(string html)
        => new(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(html),
        }));

    private sealed class PassthroughDecoder : IGoogleNewsUrlDecoder
    {
        public Task<string?> TryDecodeAsync(string googleNewsUrl, CancellationToken ct = default)
            => Task.FromResult<string?>(null);
    }

    private sealed class StubGoogleNewsUrlDecoder(string? decodedUrl) : IGoogleNewsUrlDecoder
    {
        public Task<string?> TryDecodeAsync(string googleNewsUrl, CancellationToken ct = default)
            => Task.FromResult(decodedUrl);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => responseFactory(request);
    }
}
