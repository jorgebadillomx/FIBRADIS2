using Application.Catalog;

namespace Application.Tests.Catalog;

public class FibraSlugTests
{
    [Fact]
    public void Build_BasicName_ReturnsKebabWithTickerSuffix()
    {
        Assert.Equal("fibra-uno-funo11", FibraSlug.Build("Fibra Uno", "FUNO11"));
    }

    // Tabla de la historia 11.3 — debe coincidir 1:1 con fibra-slug.test.ts (paridad C# ↔ TS)
    [Theory]
    [InlineData("Fibra Uno", "FUNO11", "fibra-uno-funo11")]
    [InlineData("Fibra Macquarie", "FIBRAMQ12", "fibra-macquarie-fibramq12")]
    [InlineData("Fibra Hotel City Express", "HCITY17", "fibra-hotel-city-express-hcity17")]
    [InlineData("CFE Fibra E", "FCFE18", "cfe-fibra-e-fcfe18")]
    [InlineData("Fibra҃uno", "FUNO11", "fibrauno-funo11")] // Mn fuera de U+0300-036F — paridad con \p{Mn} del TS/mjs
    public void Build_CatalogExamples_MatchExpectedSlugs(string fullName, string ticker, string expected)
    {
        Assert.Equal(expected, FibraSlug.Build(fullName, ticker));
    }

    [Fact]
    public void Build_NameWithAccents_NormalizesAccents()
    {
        Assert.Equal("fibra-proximamente-nu11", FibraSlug.Build("Fibra Próximamente", "NU11"));
        Assert.Equal("fibra-montana-test1", FibraSlug.Build("Fibra Montaña", "TEST1"));
    }

    [Fact]
    public void Build_NameWithSpecialChars_StripsNonAlphanumeric()
    {
        // puntuación colapsa a UN guión — misma semántica que el regex [^a-z0-9]+ del TS
        Assert.Equal("fibra-plus-s-a-fplus16", FibraSlug.Build("Fibra Plus, S.A.", "FPLUS16"));
        Assert.Equal("fibra-test-x99", FibraSlug.Build("  Fibra -- Test!  ", "X99"));
    }

    [Fact]
    public void Build_EmptyName_ReturnsTickerOnly()
    {
        Assert.Equal("funo11", FibraSlug.Build("", "FUNO11"));
        Assert.Equal("funo11", FibraSlug.Build("   ", "FUNO11"));
    }

    [Fact]
    public void Build_TickerIsLowercased()
    {
        Assert.Equal("terrafina-tera20", FibraSlug.Build("Terrafina", "TERA20"));
    }
}
