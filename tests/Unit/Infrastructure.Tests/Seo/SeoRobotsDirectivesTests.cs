using Application.Seo;

namespace Infrastructure.Tests.Seo;

public class SeoRobotsDirectivesTests
{
    [Fact]
    public void TryNormalize_WhenValueIsBlank_ReturnsEmptyString()
    {
        var success = SeoRobotsDirectives.TryNormalize(null, out var normalized, out var errors);

        Assert.True(success);
        Assert.Equal(string.Empty, normalized);
        Assert.Empty(errors);
    }

    [Fact]
    public void TryNormalize_WhenValueIsIndexableRecommended_ReturnsCanonicalString()
    {
        var success = SeoRobotsDirectives.TryNormalize(
            "index,follow,max-image-preview:large,max-snippet:-1,max-video-preview:-1",
            out var normalized,
            out var errors);

        Assert.True(success);
        Assert.Equal(SeoRobotsDirectives.IndexableRecommended, normalized);
        Assert.Empty(errors);
    }

    [Fact]
    public void TryNormalize_WhenValueIsNoIndex_ReturnsCanonicalString()
    {
        var success = SeoRobotsDirectives.TryNormalize("noindex,nofollow", out var normalized, out var errors);

        Assert.True(success);
        Assert.Equal(SeoRobotsDirectives.NoIndex, normalized);
        Assert.Empty(errors);
    }

    [Fact]
    public void TryNormalize_WhenValueIsIndexWithoutSnippet_ReturnsCanonicalString()
    {
        var success = SeoRobotsDirectives.TryNormalize("index,follow,max-snippet:0", out var normalized, out var errors);

        Assert.True(success);
        Assert.Equal(SeoRobotsDirectives.IndexWithoutSnippet, normalized);
        Assert.Empty(errors);
    }

    [Fact]
    public void TryNormalize_WhenValueContainsUnknownToken_ReturnsValidationError()
    {
        var success = SeoRobotsDirectives.TryNormalize("index,follow,foo", out var normalized, out var errors);

        Assert.False(success);
        Assert.Equal(string.Empty, normalized);
        Assert.Contains("robotsDirectives", errors.Keys);
    }

    [Fact]
    public void TryNormalize_WhenValueContainsContradictoryTokens_ReturnsValidationError()
    {
        var success = SeoRobotsDirectives.TryNormalize("index,noindex,follow", out var normalized, out var errors);

        Assert.False(success);
        Assert.Equal(string.Empty, normalized);
        Assert.Contains("index y noindex", string.Join('\n', errors["robotsDirectives"]));
    }

    [Fact]
    public void TryNormalize_WhenFollowAndNoFollowCoexist_ReturnsValidationError()
    {
        var success = SeoRobotsDirectives.TryNormalize("index,follow,nofollow", out var normalized, out var errors);

        Assert.False(success);
        Assert.Equal(string.Empty, normalized);
        Assert.Contains("follow y nofollow", string.Join('\n', errors["robotsDirectives"]));
    }

    [Theory]
    [InlineData("index,follow,max-image-preview:huge")]
    [InlineData("index,follow,max-image-preview:")]
    public void TryNormalize_WhenMaxImagePreviewIsInvalid_ReturnsValidationError(string value)
    {
        var success = SeoRobotsDirectives.TryNormalize(value, out var normalized, out var errors);

        Assert.False(success);
        Assert.Equal(string.Empty, normalized);
        Assert.Contains("robotsDirectives", errors.Keys);
    }

    [Theory]
    [InlineData("index,follow,max-snippet:abc")]
    [InlineData("index,follow,max-snippet:-5")]
    [InlineData("index,follow,max-video-preview:xyz")]
    [InlineData("index,follow,max-video-preview:-2")]
    public void TryNormalize_WhenNumericDirectiveIsInvalid_ReturnsValidationError(string value)
    {
        var success = SeoRobotsDirectives.TryNormalize(value, out var normalized, out var errors);

        Assert.False(success);
        Assert.Equal(string.Empty, normalized);
        Assert.Contains("robotsDirectives", errors.Keys);
    }

    [Fact]
    public void TryNormalize_WhenValueIsAll_MapsToIndexFollow()
    {
        var success = SeoRobotsDirectives.TryNormalize("all", out var normalized, out var errors);

        Assert.True(success);
        Assert.Equal("index,follow", normalized);
        Assert.Empty(errors);
    }

    [Fact]
    public void TryNormalize_WhenValueIsNone_MapsToNoIndexNoFollow()
    {
        var success = SeoRobotsDirectives.TryNormalize("none", out var normalized, out var errors);

        Assert.True(success);
        Assert.Equal(SeoRobotsDirectives.NoIndex, normalized);
        Assert.Empty(errors);
    }

    [Fact]
    public void TryNormalize_PreservesPassthroughDirectivesInCanonicalOrder()
    {
        var success = SeoRobotsDirectives.TryNormalize(
            "noimageindex,index,nosnippet,follow,noarchive",
            out var normalized,
            out var errors);

        Assert.True(success);
        Assert.Equal("index,follow,noarchive,nosnippet,noimageindex", normalized);
        Assert.Empty(errors);
    }
}
