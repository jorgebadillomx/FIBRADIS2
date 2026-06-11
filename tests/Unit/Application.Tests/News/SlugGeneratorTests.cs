using Application.News;

namespace Application.Tests.News;

public class SlugGeneratorTests
{
    [Fact]
    public void Generate_BasicTitle_ReturnsKebabCase()
    {
        Assert.Equal("funo11-reporta-resultados-del-2t25", SlugGenerator.Generate("FUNO11 reporta resultados del 2T25"));
    }

    [Fact]
    public void Generate_TitleWithTildes_NormalizesAccents()
    {
        Assert.Equal("funo11-noticias-o-e-a-n", SlugGenerator.Generate("FUNO11 noticias: ó, é, á, ñ"));
    }

    [Fact]
    public void Generate_TitleWithSpecialChars_StripsNonAlphanumeric()
    {
        Assert.Equal("fibra-mxbpo-2q25", SlugGenerator.Generate("FIBRA $MXBPO! — 2Q25"));
    }

    [Fact]
    public void Generate_VeryLongTitle_TruncatesAt200()
    {
        var longTitle = string.Join(' ', Enumerable.Repeat("palabra", 60)); // ~480 chars
        var slug = SlugGenerator.Generate(longTitle);

        Assert.True(slug.Length <= 200);
        Assert.False(slug.EndsWith('-'));
    }

    [Fact]
    public void Generate_EmptyTitle_ReturnsFallback()
    {
        Assert.Equal("noticia", SlugGenerator.Generate(""));
        Assert.Equal("noticia", SlugGenerator.Generate("   "));
    }

    [Fact]
    public void Generate_OnlySpecialChars_ReturnsFallback()
    {
        Assert.Equal("noticia", SlugGenerator.Generate("¡¿!? — …"));
    }

    [Fact]
    public void Generate_MultipleSpaces_CollapsesHyphens()
    {
        Assert.Equal("fibra-uno-resultados", SlugGenerator.Generate("Fibra   Uno  --  resultados"));
    }
}
