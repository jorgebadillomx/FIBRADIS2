using System.Net;
using Application.News;
using Infrastructure.Integrations.GoogleNews;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Integrations.GoogleNews;

public class GoogleNewsUrlDecoderTests
{
    [Fact]
    public async Task TryDecodeAsync_WhenGoogleNewsArticlePageContainsTokenData_ReturnsPublisherUrl()
    {
        const string googleUrl = "https://news.google.com/rss/articles/CBMilAFBVV95cUxQMVBUVVVMeHJycWcwWTBoSzM5WDdlR1h1VWx0TkNrZVBLME1rbzMyeWZrX1JYZVp4WUVKdWRFUEZFazJQMHZwWDVkWmJPNlQ3WDhlVFl0Y2JCWFZ1WWs4eElHTUNjZXdGNUNBZGotdEFOTldiRE5UTVdHM016MzZDcGZJRnZEblQwQU95NDdXek5qbmF60gGaAUFVX3lxTE9famRKZWZUdVdqNFQyclNudldyTWhMWjl0M3hvenFYSFpJalVENVhIelBNV3ZTUHRSaEFhTVZSWEstNjdPbkpNeEFzQW44b1lmU2FqTmNabFJoVGdHckUzYTJNNjcwZk9IZlNvQ2lJLS0xNDNPZGQyOEhBNkQxY2NwSGY2VXJHN2ZZNUtFbVVrZlhyRjUtWkVqM0E?oc=5";
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.Method == HttpMethod.Get && request.RequestUri!.AbsoluteUri.StartsWith("https://news.google.com/rss/articles/", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        <html>
                          <body>
                            <c-wiz>
                              <div jscontroller="X" data-n-a-ts="1779453024" data-n-a-sg="AaLI4RTZxZqWgQO-HCaT15Oun92d"></div>
                            </c-wiz>
                          </body>
                        </html>
                        """),
                });
            }

            if (request.Method == HttpMethod.Post && request.RequestUri!.AbsoluteUri == "https://news.google.com/_/DotsSplashUi/data/batchexecute")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        )]}'

                        [["wrb.fr","Fbv4je","[\"garturlres\",\"https://www.eleconomista.com.mx/opinion/fibra-danhos-pagara-dividendo-20260224-801518.html\",1,\"https://www.eleconomista.com.mx/amp/opinion/fibra-danhos-pagara-dividendo-20260224-801518.html\"]",null,null,null,""]]
                        """),
                });
            }

            throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");
        });

        var decoder = new GoogleNewsUrlDecoder(new HttpClient(handler), NullLogger<GoogleNewsUrlDecoder>.Instance);

        var decodedUrl = await decoder.TryDecodeAsync(googleUrl);

        Assert.Equal("https://www.eleconomista.com.mx/opinion/fibra-danhos-pagara-dividendo-20260224-801518.html", decodedUrl);
    }

    [Fact]
    public async Task TryDecodeAsync_WhenUrlIsNotGoogleNews_ReturnsNull()
    {
        var decoder = new GoogleNewsUrlDecoder(
            new HttpClient(new StubHttpMessageHandler(_ => throw new InvalidOperationException("Should not call HTTP"))),
            NullLogger<GoogleNewsUrlDecoder>.Instance);

        var decodedUrl = await decoder.TryDecodeAsync("https://www.eleconomista.com.mx/opinion/fibra-danhos-pagara-dividendo-20260224-801518.html");

        Assert.Null(decodedUrl);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => responseFactory(request);
    }
}
