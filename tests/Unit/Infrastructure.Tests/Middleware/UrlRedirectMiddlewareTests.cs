using Api.Middleware;
using Application.Seo;
using Domain.Seo;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests.Middleware;

public class UrlRedirectMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ActiveRedirect_PreservesQueryString()
    {
        var (context, nextCalled) = await InvokeAsync("/blog", "?utm_source=google");

        Assert.False(nextCalled.Value);
        Assert.Equal(StatusCodes.Status301MovedPermanently, context.Response.StatusCode);
        Assert.Equal("/noticias?utm_source=google", context.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task InvokeAsync_MissingRedirect_PassesThrough()
    {
        var (context, nextCalled) = await InvokeAsync("/sin-regla");

        Assert.True(nextCalled.Value);
        Assert.False(context.Response.Headers.ContainsKey("Location"));
    }

    [Fact]
    public async Task InvokeAsync_NonGetMethod_PassesThrough()
    {
        var (context, nextCalled) = await InvokeAsync("/blog", method: "POST");

        Assert.True(nextCalled.Value);
        Assert.False(context.Response.Headers.ContainsKey("Location"));
    }

    [Theory]
    [InlineData("/api/v1/fibras")]
    [InlineData("/ops/dashboard")]
    [InlineData("/hangfire/jobs")]
    [InlineData("/assets/index-TEST.js")]
    public async Task InvokeAsync_ReservedOrAssetPath_PassesThrough(string path)
    {
        var (_, nextCalled) = await InvokeAsync(path);

        Assert.True(nextCalled.Value);
    }

    private static async Task<(DefaultHttpContext Context, StrongBox<bool> NextCalled)> InvokeAsync(
        string path,
        string queryString = "",
        string method = "GET")
    {
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddScoped<IRedirectRepository>(_ => new FakeRedirectRepository(
            CreateRedirect("/blog", "/noticias", 301, true),
            CreateRedirect("/catalogo", "/fibras", 301, false)));
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var cache = provider.GetRequiredService<IMemoryCache>();

        var nextCalled = new StrongBox<bool>(false);
        var middleware = new UrlRedirectMiddleware(
            _ =>
            {
                nextCalled.Value = true;
                return Task.CompletedTask;
            },
            scopeFactory,
            cache);

        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        if (queryString.Length > 0)
            context.Request.QueryString = new QueryString(queryString);

        await middleware.InvokeAsync(context);
        return (context, nextCalled);
    }

    private static UrlRedirect CreateRedirect(string fromPath, string toPath, int statusCode, bool isActive) => new()
    {
        Id = Guid.NewGuid(),
        FromPath = fromPath,
        ToPath = toPath,
        StatusCode = statusCode,
        IsActive = isActive,
        CreatedAt = DateTimeOffset.UtcNow,
        CreatedBy = "system",
        UpdatedAt = DateTimeOffset.UtcNow,
        UpdatedBy = "system",
    };

    private sealed class StrongBox<T>(T value)
    {
        public T Value { get; set; } = value;
    }

    private sealed class FakeRedirectRepository(params UrlRedirect[] redirects) : IRedirectRepository
    {
        public Task<IReadOnlyList<UrlRedirect>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<UrlRedirect>>(redirects.ToList());

        public Task<IReadOnlyList<UrlRedirect>> GetActiveAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<UrlRedirect>>(redirects.Where(r => r.IsActive).ToList());

        public Task<UrlRedirect?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(redirects.FirstOrDefault(r => r.Id == id));

        public Task<UrlRedirect?> GetByFromPathAsync(string fromPath, CancellationToken ct = default) =>
            Task.FromResult(redirects.FirstOrDefault(r => r.FromPath == UrlRedirectPath.Normalize(fromPath)));

        public Task AddAsync(UrlRedirect redirect, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateAsync(UrlRedirect redirect, CancellationToken ct = default) => throw new NotImplementedException();
    }
}
