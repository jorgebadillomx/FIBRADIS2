using Api.CompositionRoot;
using Api.Endpoints.Ops;
using Api.Endpoints.Private;
using Api.Endpoints.Public;
using Api.Middleware;
using Application.Ops;
using Hangfire;
using Infrastructure.Jobs.Fundamentals;
using Infrastructure.Jobs.Market;
using Infrastructure.Jobs.News;
using Infrastructure.Persistence.SqlServer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiInfrastructure();
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status301MovedPermanently;
});
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});
builder.WebHost.ConfigureKestrel(o => o.AddServerHeader = false);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.UseForwardedHeaders();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    context.Response.Headers.Remove("X-Powered-By");
    await next();
});
app.UseMiddleware<WwwToNonWwwMiddleware>();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
// El 301 a la URL slug canónica debe resolverse ANTES de servir HTML (SpaMetadataMiddleware)
app.UseMiddleware<FibraSlugRedirectMiddleware>();
// /fibras/{slug} dinámico — después del 301 canónico, antes que SpaMetadataMiddleware (que cubre /fibras listado)
app.UseMiddleware<FibraProfileMetadataMiddleware>();
// /noticias/{slug|guid} dinámico — antes que SpaMetadataMiddleware (que cubre /noticias listado)
app.UseMiddleware<NewsMetadataMiddleware>();
app.UseMiddleware<SpaMetadataMiddleware>();
app.UseDefaultFiles();
// Assets con content-hash de Vite (en /assets/) → cache inmutable de 1 año
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var path = ctx.Context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase))
            ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
    }
});
app.UseRouting(); // explícito: debe ir después de static files para que los assets no sean interceptados por el fallback
app.UseApiInfrastructure();
app.MapAuth();
app.MapMe();
app.MapOpsPing();
app.MapOpsMarket();
app.MapOpsDashboard();
app.MapAiMode();
app.MapAiProvider();
app.MapOpsAiPrompts();
app.MapOpsPipelineLogs();
app.MapOpsAiCallLogs();
app.MapNewsBlocklist();
app.MapNews();
app.MapEditorial();
app.MapCatalog();
app.MapMarket();
app.MapOpsFundamentals();
app.MapOpsCatalog();
app.MapOpsConfig();
app.MapOpsEditorial();
app.MapFundamentalsPublic();
app.MapCompare();
app.MapPortfolio();
app.MapFavorites();
app.MapOpportunities();
app.MapOpsUsers();
app.MapAccount();
app.MapSeo();
app.MapOpsNewsManagement();


app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("/ops/{**slug}", "ops/index.html");
app.MapFallbackToFile("index.html");

var useInMemoryHangfire = builder.Configuration.GetValue<bool>("Hangfire:UseInMemoryStorage");
var hangfireConnStr = builder.Configuration.GetConnectionString("DefaultConnection");
var skipStartupDbReads = string.Equals(
    Environment.GetEnvironmentVariable("FIBRADIS_SKIP_STARTUP_DB_READS"),
    "1",
    StringComparison.Ordinal);
if (!useInMemoryHangfire && !string.IsNullOrEmpty(hangfireConnStr))
{
    var mexicoTz = MarketPipelineSchedule.GetMexicoTimeZone();

    foreach (var (jobId, cronExpression) in MarketPipelineSchedule.GetRecurringJobs())
    {
        RecurringJob.AddOrUpdate<MarketPipelineJob>(
            jobId,
            j => j.ExecuteAsync(CancellationToken.None),
            cronExpression,
            new RecurringJobOptions { TimeZone = mexicoTz });
    }

    RecurringJob.AddOrUpdate<NewsPipelineJob>(
        NewsPipelineSchedule.HourlyJobId,
        j => j.ExecuteAsync(CancellationToken.None),
        NewsPipelineSchedule.CronExpression,
        new RecurringJobOptions { TimeZone = mexicoTz });

    if (!skipStartupDbReads)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var opConfig = await scope.ServiceProvider
                .GetRequiredService<IOperationalConfigRepository>()
                .GetAsync();

            RecurringJob.AddOrUpdate<NewsPipelineJob>(
                NewsPipelineSchedule.HourlyJobId,
                j => j.ExecuteAsync(CancellationToken.None),
                NewsPipelineSchedule.GetCronExpression(opConfig.NewsCadenceMinutes),
                new RecurringJobOptions { TimeZone = mexicoTz });
        }
        catch (Exception ex)
        {
            var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
            startupLogger.LogError(ex, "No se pudo leer NewsCadenceMinutes desde BD al arranque. Usando default.");
        }
    }

    RecurringJob.AddOrUpdate<DistributionPipelineJob>(
        MarketPipelineSchedule.DistributionJobId,
        j => j.ExecuteAsync(CancellationToken.None),
        MarketPipelineSchedule.DistributionCronExpression,
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

    var fundamentalsCadenceMinutes = FundamentalsPipelineSchedule.DefaultCadenceMinutes;
    if (!skipStartupDbReads)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            fundamentalsCadenceMinutes = (await scope.ServiceProvider
                .GetRequiredService<IOperationalConfigRepository>()
                .GetAsync()).FundamentalsCadenceMinutes;
        }
        catch (Exception ex)
        {
            var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
            startupLogger.LogError(ex, "No se pudo leer FundamentalsCadenceMinutes desde BD al arranque. Usando default.");
        }
    }

    RecurringJob.AddOrUpdate<FundamentalsPipelineJob>(
        FundamentalsPipelineSchedule.JobId,
        j => j.ExecuteAsync(false, CancellationToken.None),
        FundamentalsPipelineSchedule.GetCronExpression(fundamentalsCadenceMinutes),
        new RecurringJobOptions { TimeZone = mexicoTz });
}

app.Run();

public partial class Program { }
