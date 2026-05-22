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
