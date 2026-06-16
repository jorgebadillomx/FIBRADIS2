using Api.Seo;

namespace Infrastructure.Tests.Seo;

public class SpaRouteCatalogTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/fibras")]
    [InlineData("/noticias")]
    [InlineData("/comparar")]
    [InlineData("/fundamentales")]
    [InlineData("/plataforma")]
    [InlineData("/herramientas")]
    [InlineData("/perfil")]
    [InlineData("/portafolio")]
    public void IsKnownSpaRoute_ExactKnownRoute_ReturnsTrue(string path)
    {
        Assert.True(SpaRouteCatalog.IsKnownSpaRoute(path));
    }

    [Theory]
    [InlineData("/Fibras")]
    [InlineData("/NOTICIAS")]
    [InlineData("/fibras/")]
    [InlineData("/noticias/")]
    public void IsKnownSpaRoute_KnownRoute_IsCaseAndTrailingSlashInsensitive(string path)
    {
        Assert.True(SpaRouteCatalog.IsKnownSpaRoute(path));
    }

    [Theory]
    [InlineData("/fibras/funo11")]
    [InlineData("/noticias/algun-slug-de-noticia")]
    public void IsKnownSpaRoute_DynamicPrefix_ReturnsTrue(string path)
    {
        // El slug concreto lo valida su middleware; aquí solo reconocemos el prefijo dinámico.
        Assert.True(SpaRouteCatalog.IsKnownSpaRoute(path));
    }

    [Theory]
    [InlineData("/esta-pagina-no-existe-xyz123")]
    [InlineData("/portafolio/algo")]
    [InlineData("/random/deep/path")]
    [InlineData("/fibrasx")]
    [InlineData("/foo.js")]
    public void IsKnownSpaRoute_UnknownRoute_ReturnsFalse(string path)
    {
        Assert.False(SpaRouteCatalog.IsKnownSpaRoute(path));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsKnownSpaRoute_NullOrEmpty_TreatedAsRoot(string? path)
    {
        Assert.True(SpaRouteCatalog.IsKnownSpaRoute(path));
    }
}
