using Api.Middleware;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Tests.Middleware;

public class WwwToNonWwwMiddlewareTests
{
    [Fact]
    public async Task Returns301_WhenHostStartsWithWww_Http()
    {
        var context = CreateContext("http", "www.fibrasinmobiliarias.com");
        var middleware = new WwwToNonWwwMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status301MovedPermanently, context.Response.StatusCode);
        Assert.Equal("https://fibrasinmobiliarias.com/", context.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Returns301_WhenHostStartsWithWww_Https()
    {
        var context = CreateContext("https", "www.fibrasinmobiliarias.com", "/fibras/FUNO11");
        var middleware = new WwwToNonWwwMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status301MovedPermanently, context.Response.StatusCode);
        Assert.Equal("https://fibrasinmobiliarias.com/fibras/FUNO11", context.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task PassesThrough_WhenHostIsNonWww()
    {
        var nextCalled = false;
        var context = CreateContext("https", "fibrasinmobiliarias.com");
        var middleware = new WwwToNonWwwMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.False(context.Response.Headers.ContainsKey("Location"));
    }

    [Fact]
    public async Task PreservesPathAndQuery()
    {
        var context = CreateContext("http", "www.fibrasinmobiliarias.com", "/calculadora", "?utm_source=google&utm_medium=cpc");
        var middleware = new WwwToNonWwwMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal("https://fibrasinmobiliarias.com/calculadora?utm_source=google&utm_medium=cpc", context.Response.Headers.Location.ToString());
    }

    private static DefaultHttpContext CreateContext(string scheme, string host, string path = "/", string queryString = "")
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = scheme;
        context.Request.Host = new HostString(host);
        context.Request.Path = path;
        context.Request.QueryString = new QueryString(queryString);
        return context;
    }
}
