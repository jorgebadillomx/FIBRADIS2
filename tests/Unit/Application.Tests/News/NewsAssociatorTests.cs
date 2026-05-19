using Application.News;

namespace Application.Tests.News;

public class NewsAssociatorTests
{
    private static readonly Guid FunoId = Guid.NewGuid();
    private static readonly Guid DanhosId = Guid.NewGuid();

    [Fact]
    public void Associate_MatchesByTickerInTitle()
    {
        var item = new RssItem("FUNO11 acelera su expansión", "Fuente", DateTimeOffset.UtcNow, "https://example.com/1", null);

        var result = NewsAssociator.Associate(item, [CreateFibra(FunoId, "FUNO11", "Fibra Uno")]);

        Assert.Contains(FunoId, result);
    }

    [Fact]
    public void Associate_MatchesByTickerInSnippet()
    {
        var item = new RssItem("Expansión del sector", "Fuente", DateTimeOffset.UtcNow, "https://example.com/2", "FUNO11 anuncia inversión");

        var result = NewsAssociator.Associate(item, [CreateFibra(FunoId, "FUNO11", "Fibra Uno")]);

        Assert.Contains(FunoId, result);
    }

    [Fact]
    public void Associate_MatchesByNameVariant()
    {
        var item = new RssItem("Fibra Uno mejora ocupación", "Fuente", DateTimeOffset.UtcNow, "https://example.com/3", null);

        var result = NewsAssociator.Associate(item, [CreateFibra(FunoId, "FUNO11", "Fibra Uno")]);

        Assert.Contains(FunoId, result);
    }

    [Fact]
    public void Associate_IsCaseInsensitive()
    {
        var item = new RssItem("funo11 mejora guidance", "Fuente", DateTimeOffset.UtcNow, "https://example.com/4", null);

        var result = NewsAssociator.Associate(item, [CreateFibra(FunoId, "FUNO11", "Fibra Uno")]);

        Assert.Contains(FunoId, result);
    }

    [Fact]
    public void Associate_NoMatchReturnsEmpty()
    {
        var item = new RssItem("Mercado industrial mexicano", "Fuente", DateTimeOffset.UtcNow, "https://example.com/5", "Sin emisoras específicas");

        var result = NewsAssociator.Associate(item, [CreateFibra(FunoId, "FUNO11", "Fibra Uno")]);

        Assert.Empty(result);
    }

    [Fact]
    public void Associate_MultipleMatches()
    {
        var item = new RssItem("FUNO11 y DANHOS13 anuncian proyectos", "Fuente", DateTimeOffset.UtcNow, "https://example.com/6", null);

        var result = NewsAssociator.Associate(item,
        [
            CreateFibra(FunoId, "FUNO11", "Fibra Uno"),
            CreateFibra(DanhosId, "DANHOS13", "Danhos"),
        ]);

        Assert.Equal(2, result.Count);
        Assert.Contains(FunoId, result);
        Assert.Contains(DanhosId, result);
    }

    [Fact]
    public void Associate_TickerSubstringDoesNotFalsePositive()
    {
        var item = new RssItem("FUNO11 mantiene guidance", "Fuente", DateTimeOffset.UtcNow, "https://example.com/7", null);

        var result = NewsAssociator.Associate(item, [CreateFibra(FunoId, "UN", "Un Fibra")]);

        Assert.Empty(result);
    }

    [Fact]
    public void Associate_NameVariantSubstringDoesNotFalsePositive()
    {
        var item = new RssItem("Superfibra Uno mantiene guidance", "Fuente", DateTimeOffset.UtcNow, "https://example.com/8", null);

        var result = NewsAssociator.Associate(item, [CreateFibra(FunoId, "FUNO11", "Fibra Uno")]);

        Assert.Empty(result);
    }

    private static FibraMatchInfo CreateFibra(Guid id, string ticker, params string[] variants)
        => new(id, ticker, variants);
}
