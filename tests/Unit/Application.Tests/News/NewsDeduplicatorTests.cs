using Application.News;

namespace Application.Tests.News;

public class NewsDeduplicatorTests
{
    [Fact]
    public void NormalizeTitle_LowercasesAndRemovesPunctuation()
    {
        Assert.Equal("funo11 fibra", NewsDeduplicator.NormalizeTitle("FUNO11, Fibra!"));
    }

    [Fact]
    public void MatchesBlocklist_ReturnsTrueWhenTitleContainsTerm()
    {
        var result = NewsDeduplicator.MatchesBlocklist("noticias fibra óptica", null, ["fibra óptica"]);
        Assert.True(result);
    }

    [Fact]
    public void MatchesBlocklist_ReturnsTrueWhenSnippetContainsTerm()
    {
        var result = NewsDeduplicator.MatchesBlocklist("FUNO11 sube", "reporte de fibra óptica", ["fibra óptica"]);
        Assert.True(result);
    }

    [Fact]
    public void MatchesBlocklist_ReturnsFalseWhenNoMatch()
    {
        var result = NewsDeduplicator.MatchesBlocklist("FUNO11 sube 3%", null, ["fibra óptica"]);
        Assert.False(result);
    }

    [Fact]
    public void MatchesBlocklist_IsCaseInsensitive()
    {
        var result = NewsDeduplicator.MatchesBlocklist("FIBRA OPTICA en la red", null, ["fibra optica"]);
        Assert.True(result);
    }

    [Fact]
    public void Filter_RemovesItemsMatchingBlocklist()
    {
        var items = new[]
        {
            new RssItem("fibra óptica para hogares", "Fuente", DateTimeOffset.UtcNow, "https://example.com/1", null),
        };

        var result = NewsDeduplicator.Filter(items, new HashSet<string>(StringComparer.OrdinalIgnoreCase), [], ["fibra óptica"]);

        Assert.Empty(result);
    }

    [Fact]
    public void Filter_RemovesExactDuplicateUrl()
    {
        var items = new[]
        {
            new RssItem("FUNO11 alza guidance", "Fuente", DateTimeOffset.UtcNow, "https://example.com/1", null),
            new RssItem("FUNO11 confirma guidance", "Fuente", DateTimeOffset.UtcNow, "https://example.com/1", null),
        };

        var result = NewsDeduplicator.Filter(items, new HashSet<string>(StringComparer.OrdinalIgnoreCase), [], []);

        Assert.Single(result);
        Assert.Equal("FUNO11 alza guidance", result[0].Title);
    }

    [Fact]
    public void Filter_RemovesProbableDuplicateTitle()
    {
        var items = new[]
        {
            new RssItem("FUNO11 sube, 3%!", "Fuente", DateTimeOffset.UtcNow, "https://example.com/1", null),
            new RssItem("FUNO11 sube 3%", "Fuente", DateTimeOffset.UtcNow, "https://example.com/2", null),
        };

        var result = NewsDeduplicator.Filter(items, new HashSet<string>(StringComparer.OrdinalIgnoreCase), [], []);

        Assert.Single(result);
        Assert.Equal("https://example.com/1", result[0].Url);
    }

    [Fact]
    public void Filter_KeepsTitleDupOutside24hWindow()
    {
        var items = new[]
        {
            new RssItem("Fibra Uno eleva ocupación", "Fuente", DateTimeOffset.UtcNow, "https://example.com/1", null),
        };

        var result = NewsDeduplicator.Filter(
            items,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            [],
            []);

        Assert.Single(result);
    }

    [Fact]
    public void Filter_AllowsCleanItemThrough()
    {
        var items = new[]
        {
            new RssItem("FMTY14 anuncia expansión", "Fuente", DateTimeOffset.UtcNow, "https://example.com/clean", "sin bloqueos"),
        };

        var result = NewsDeduplicator.Filter(
            items,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            ["otro titulo"],
            ["fibra óptica"]);

        Assert.Single(result);
        Assert.Equal("https://example.com/clean", result[0].Url);
    }

    [Fact]
    public void Filter_AllowsMultipleUniqueUrlsWhenTitleIsEmpty()
    {
        var items = new[]
        {
            new RssItem("", "Fuente", DateTimeOffset.UtcNow, "https://example.com/empty-1", null),
            new RssItem("", "Fuente", DateTimeOffset.UtcNow, "https://example.com/empty-2", null),
        };

        var result = NewsDeduplicator.Filter(
            items,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            [],
            []);

        Assert.Equal(2, result.Count);
    }
}
