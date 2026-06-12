using Api.Seo;

namespace Infrastructure.Tests.Seo;

public class SpaMetadataProviderTests
{
    private readonly SpaMetadataProvider _provider = new();

    [Theory]
    [InlineData("/")]
    [InlineData("/calculadora")]
    [InlineData("/comparar")]
    [InlineData("/fibras")]
    [InlineData("/noticias")]
    [InlineData("/conoce-las-fibras")]
    [InlineData("/calendario")]
    [InlineData("/fundamentales")]
    [InlineData("/privacidad")]
    [InlineData("/acerca")]
    [InlineData("/contacto")]
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
    [InlineData("/herramientas")]
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
        [InlineData("/fibras")]
        [InlineData("/noticias")]
        [InlineData("/conoce-las-fibras")]
        [InlineData("/calendario")]
        [InlineData("/fundamentales")]
        [InlineData("/privacidad")]
        [InlineData("/acerca")]
        [InlineData("/contacto")]
    public void Descriptions_AreBetween120And160Chars(string path)
    {
        var meta = _provider.GetMetaForPath(path);

        Assert.NotNull(meta);
        Assert.InRange(meta.Description.Length, 120, 160);
    }

    [Fact]
    public void Calculadora_HasSoftwareApplicationJsonLd()
    {
        var meta = _provider.GetMetaForPath("/calculadora");

        Assert.NotNull(meta);
        Assert.NotNull(meta.JsonLd);
        Assert.Contains("\"@type\": \"SoftwareApplication\"", meta.JsonLd);
        Assert.Contains("\"name\": \"Calculadora de compra de FIBRAs\"", meta.JsonLd);
        Assert.Contains("\"description\": \"Calcula cuántos CBFIs puedes comprar con tu presupuesto", meta.JsonLd);
    }

    [Fact]
    public void Homepage_HasOrganizationAndWebSiteJsonLd()
    {
        var meta = _provider.GetMetaForPath("/");

        Assert.NotNull(meta);
        Assert.NotNull(meta.JsonLd);
        Assert.Contains("\"@type\": \"Organization\"", meta.JsonLd);
        Assert.Contains("\"@type\": \"WebSite\"", meta.JsonLd);
    }

    [Theory]
    [InlineData("/noticias")]
    [InlineData("/fibras")]
    [InlineData("/comparar")]
    public void ContentRoutes_HaveNoJsonLd(string path)
    {
        Assert.Null(_provider.GetMetaForPath(path)!.JsonLd);
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
