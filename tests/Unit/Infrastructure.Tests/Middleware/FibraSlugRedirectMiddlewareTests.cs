using Api.Middleware;
using Application.Catalog;
using Domain.Catalog;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests.Middleware;

public class FibraSlugRedirectMiddlewareTests
{
    private static readonly Fibra Funo = new()
    {
        Id = Guid.NewGuid(),
        Ticker = "FUNO11",
        FullName = "Fibra Uno",
        State = FibraState.Active,
    };

    [Fact]
    public async Task InvokeAsync_BareTicker_Redirects301ToSlug()
    {
        var (context, nextCalled) = await InvokeAsync("/fibras/FUNO11");

        Assert.False(nextCalled.Value);
        Assert.Equal(StatusCodes.Status301MovedPermanently, context.Response.StatusCode);
        Assert.Equal("/fibras/fibra-uno-funo11", context.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task InvokeAsync_LowercaseTicker_Redirects301ToSlug()
    {
        var (context, nextCalled) = await InvokeAsync("/fibras/funo11");

        Assert.False(nextCalled.Value);
        Assert.Equal(StatusCodes.Status301MovedPermanently, context.Response.StatusCode);
        Assert.Equal("/fibras/fibra-uno-funo11", context.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task InvokeAsync_CanonicalSlug_PassesThrough()
    {
        var (context, nextCalled) = await InvokeAsync("/fibras/fibra-uno-funo11");

        Assert.True(nextCalled.Value);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.False(context.Response.Headers.ContainsKey("Location"));
    }

    [Fact]
    public async Task InvokeAsync_StaleSlugValidTicker_Redirects301ToCanonical()
    {
        // el nombre cambió pero el ticker resuelve — la URL vieja sigue funcionando vía 301
        var (context, nextCalled) = await InvokeAsync("/fibras/nombre-viejo-funo11");

        Assert.False(nextCalled.Value);
        Assert.Equal(StatusCodes.Status301MovedPermanently, context.Response.StatusCode);
        Assert.Equal("/fibras/fibra-uno-funo11", context.Response.Headers.Location.ToString());
    }

    [Theory]
    [InlineData("/fibras/fibra-uno-funo11/")] // trailing slash
    [InlineData("/Fibras/fibra-uno-funo11")] // prefijo con mayúsculas (react-router lo renderiza 200)
    [InlineData("/fibras/FUNO11/")] // ticker pelado con trailing slash
    public async Task InvokeAsync_NonCanonicalVariants_Redirect301ToCanonical(string path)
    {
        var (context, nextCalled) = await InvokeAsync(path);

        Assert.False(nextCalled.Value);
        Assert.Equal(StatusCodes.Status301MovedPermanently, context.Response.StatusCode);
        Assert.Equal("/fibras/fibra-uno-funo11", context.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task InvokeAsync_UnknownTicker_PassesThrough()
    {
        // la SPA muestra FibraNotFound
        var (context, nextCalled) = await InvokeAsync("/fibras/slug-con-ticker-inexistente-xyz99");

        Assert.True(nextCalled.Value);
        Assert.False(context.Response.Headers.ContainsKey("Location"));
    }

    [Theory]
    [InlineData("/assets/index-1TzwM6fE.js")]
    [InlineData("/fibras/funo11.json")]
    [InlineData("/api/v1/fibras/FUNO11")]
    [InlineData("/ops/dashboard")]
    [InlineData("/hangfire/jobs")]
    [InlineData("/calculadora")]
    [InlineData("/fibras/funo11/algo-mas")]
    public async Task InvokeAsync_AssetOrApiPath_PassesThrough(string path)
    {
        var (context, nextCalled) = await InvokeAsync(path);

        Assert.True(nextCalled.Value);
        Assert.False(context.Response.Headers.ContainsKey("Location"));
    }

    [Fact]
    public async Task InvokeAsync_NonGetMethod_PassesThrough()
    {
        var (context, nextCalled) = await InvokeAsync("/fibras/FUNO11", method: "POST");

        Assert.True(nextCalled.Value);
        Assert.False(context.Response.Headers.ContainsKey("Location"));
    }

    [Fact]
    public async Task InvokeAsync_HeadRequest_Redirects301ToSlug()
    {
        // curl -I y validadores SEO usan HEAD — mismo 301 que GET
        var (context, nextCalled) = await InvokeAsync("/fibras/FUNO11", method: "HEAD");

        Assert.False(nextCalled.Value);
        Assert.Equal(StatusCodes.Status301MovedPermanently, context.Response.StatusCode);
        Assert.Equal("/fibras/fibra-uno-funo11", context.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task InvokeAsync_PreservesQueryString()
    {
        var (context, _) = await InvokeAsync("/fibras/FUNO11", queryString: "?utm_source=google");

        Assert.Equal(StatusCodes.Status301MovedPermanently, context.Response.StatusCode);
        Assert.Equal("/fibras/fibra-uno-funo11?utm_source=google", context.Response.Headers.Location.ToString());
    }

    private static async Task<(DefaultHttpContext Context, StrongBox<bool> NextCalled)> InvokeAsync(
        string path,
        string method = "GET",
        string queryString = "")
    {
        var services = new ServiceCollection();
        services.AddScoped<IFibraRepository>(_ => new FakeFibraRepository(Funo));
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        var nextCalled = new StrongBox<bool>(false);
        var middleware = new FibraSlugRedirectMiddleware(
            _ =>
            {
                nextCalled.Value = true;
                return Task.CompletedTask;
            },
            scopeFactory);

        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        if (queryString.Length > 0)
            context.Request.QueryString = new QueryString(queryString);

        await middleware.InvokeAsync(context);
        return (context, nextCalled);
    }

    private sealed class StrongBox<T>(T value)
    {
        public T Value { get; set; } = value;
    }

    private sealed class FakeFibraRepository(params Fibra[] fibras) : IFibraRepository
    {
        public Task<Fibra?> GetByTickerAsync(string ticker, CancellationToken ct = default) =>
            Task.FromResult(fibras.FirstOrDefault(f =>
                string.Equals(f.Ticker, ticker, StringComparison.OrdinalIgnoreCase)));

        public Task AddAsync(Fibra fibra, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateAsync(Fibra fibra, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> ExistsByTickerAsync(string ticker, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<(IReadOnlyList<Fibra> Items, int Total)> GetActivePagedAsync(FibraFilter filter, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Fibra?> GetByIdAsync(Guid id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Fibra>> GetAllAsync(CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Fibra>> GetAllActiveAsync(CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<(string FullName, string Ticker)>> GetAllActiveForSitemapAsync(CancellationToken ct = default) => throw new NotImplementedException();
    }
}
