using System.Text.Json;
using Application.Catalog;

namespace Application.Tests.Catalog;

public class FibraSlugTests
{
    [Fact]
    public void Build_BasicName_ReturnsKebabWithTickerSuffix()
    {
        Assert.Equal("fibra-uno-funo11", FibraSlug.Build("Fibra Uno", "FUNO11"));
    }

    // Corpus de paridad COMPARTIDO con fibra-slug.test.ts (TS). Fuente única:
    // src/Web/Main/src/shared/lib/slug-parity.fixture.json (vinculado como Content en el .csproj).
    // Ambos lenguajes verifican el MISMO archivo ⇒ no puede haber drift silencioso entre
    // FibraSlug.Build (C#) y buildFibraSlug (TS). Cubre acentos, eñes, puntuación (S.A.→s-a),
    // marcas combinantes fuera de U+0300-036F, espacios/guiones múltiples y nombre vacío.
    [Fact]
    public void Build_MatchesSharedParityFixture()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "slug-parity.fixture.json");
        Assert.True(File.Exists(path), $"No se encontró el fixture de paridad en {path}");

        var fixture = JsonSerializer.Deserialize<ParityFixture>(
            File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(fixture);
        Assert.NotEmpty(fixture!.Cases);

        foreach (var c in fixture.Cases)
        {
            Assert.Equal(c.Expected, FibraSlug.Build(c.FullName, c.Ticker));
        }
    }

    [Fact]
    public void Build_TickerIsLowercased()
    {
        Assert.Equal("terrafina-tera20", FibraSlug.Build("Terrafina", "TERA20"));
    }

    private sealed record ParityFixture(List<ParityCase> Cases);
    private sealed record ParityCase(string FullName, string Ticker, string Expected);
}
