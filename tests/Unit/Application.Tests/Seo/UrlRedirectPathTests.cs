using Application.Seo;
using Xunit;

namespace Application.Tests.Seo;

public class UrlRedirectPathTests
{
    [Theory]
    [InlineData("/Blog/", "/blog")]
    [InlineData("  /Noticias  ", "/noticias")]
    [InlineData("/", "/")]                       // raíz se preserva, no se recorta a vacío
    [InlineData("/AVISO-DE-PRIVACIDAD", "/aviso-de-privacidad")]
    [InlineData("", "")]
    public void Normalize_LowercasesTrimsAndStripsTrailingSlash(string input, string expected)
    {
        Assert.Equal(expected, UrlRedirectPath.Normalize(input));
    }

    [Theory]
    [InlineData("/api")]
    [InlineData("/api/v1/secret")]
    [InlineData("/ops")]
    [InlineData("/ops/dashboard")]
    [InlineData("/fibras")]
    [InlineData("/fibras/funo11")]
    [InlineData("/hangfire")]
    [InlineData("/assets/app.js")]
    [InlineData("/")]
    public void IsReservedSource_TrueForReservedExactAndPrefixes(string normalizedPath)
    {
        Assert.True(UrlRedirectPath.IsReservedSource(normalizedPath));
    }

    [Theory]
    [InlineData("/blog")]
    [InlineData("/noticias")]
    [InlineData("/apixyz")]                      // no es prefijo "/api/" — off-by-one
    [InlineData("/fibrasiniciales")]
    public void IsReservedSource_FalseForNonReservedPaths(string normalizedPath)
    {
        Assert.False(UrlRedirectPath.IsReservedSource(normalizedPath));
    }

    [Theory]
    [InlineData("/blog")]
    [InlineData("/noticias")]
    [InlineData("/a/b/c")]
    public void IsInternalPath_TrueForInternalPaths(string value)
    {
        Assert.True(UrlRedirectPath.IsInternalPath(value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("blog")]                          // no empieza con '/'
    [InlineData("http://evil.com")]
    [InlineData("https://evil.com")]
    [InlineData("//evil.com")]                    // protocol-relative
    [InlineData("/\\evil.com")]                   // backslash bypass → navegadores lo tratan como "//"
    [InlineData("/\\/evil.com")]
    [InlineData("/\tevil.com")]                   // carácter de control
    [InlineData("/path\\with\\backslash")]        // cualquier backslash interno
    public void IsInternalPath_FalseForExternalOrUnsafePaths(string value)
    {
        Assert.False(UrlRedirectPath.IsInternalPath(value));
    }
}
