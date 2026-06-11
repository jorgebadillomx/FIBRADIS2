using Api.Seo;

namespace Infrastructure.Tests.Seo;

public class SpaMetadataProviderTests
{
    private readonly SpaMetadataProvider _provider = new();

    [Theory]
    [InlineData("/")]
    [InlineData("/calculadora")]
    [InlineData("/comparar")]
    [InlineData("/catalogo")]
    [InlineData("/noticias")]
    [InlineData("/conoce-las-fibras")]
    [InlineData("/calendario")]
    [InlineData("/fundamentales")]
    [InlineData("/herramientas")]
    public void GetMetaForPath_ReturnsMeta_ForKnownRoutes(string path)
    {
        var meta = _provider.GetMetaForPath(path);

        Assert.NotNull(meta);
        Assert.EndsWith("| FIBRADIS", meta.Title);
        Assert.Equal(path, meta.CanonicalPath);
    }

    [Theory]
    [InlineData("/portafolio")]
    [InlineData("/oportunidades")]
    [InlineData("/fibras/FUNO11")]
    [InlineData("/noticias/abc-123")]
    [InlineData("/login")]
    public void GetMetaForPath_ReturnsNull_ForUnknownRoutes(string path)
    {
        Assert.Null(_provider.GetMetaForPath(path));
    }

    [Theory]
    [InlineData("/calculadora/")]
    [InlineData("/CALCULADORA")]
    [InlineData("/Calculadora/")]
    public void GetMetaForPath_NormalizesTrailingSlashAndCase(string path)
    {
        var meta = _provider.GetMetaForPath(path);

        Assert.NotNull(meta);
        Assert.Equal("/calculadora", meta.CanonicalPath);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/calculadora")]
    [InlineData("/comparar")]
    [InlineData("/catalogo")]
    [InlineData("/noticias")]
    [InlineData("/conoce-las-fibras")]
    [InlineData("/calendario")]
    [InlineData("/fundamentales")]
    [InlineData("/herramientas")]
    public void Descriptions_AreBetween120And160Chars(string path)
    {
        var meta = _provider.GetMetaForPath(path);

        Assert.NotNull(meta);
        Assert.InRange(meta.Description.Length, 120, 160);
    }

    [Fact]
    public void Calculadora_HasFaqPageJsonLd_WithAtLeastTwoQuestions()
    {
        var meta = _provider.GetMetaForPath("/calculadora");

        Assert.NotNull(meta);
        Assert.NotNull(meta.JsonLd);
        Assert.Contains("\"@type\": \"FAQPage\"", meta.JsonLd);
        Assert.True(CountOccurrences(meta.JsonLd, "\"@type\": \"Question\"") >= 2);
    }

    [Fact]
    public void OtherRoutes_HaveNoJsonLd()
    {
        Assert.Null(_provider.GetMetaForPath("/")!.JsonLd);
        Assert.Null(_provider.GetMetaForPath("/noticias")!.JsonLd);
    }

    private static int CountOccurrences(string text, string token)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }

        return count;
    }
}
