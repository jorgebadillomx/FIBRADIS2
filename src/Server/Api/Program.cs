using Api.CompositionRoot;
using Api.Endpoints.Ops;
using Api.Endpoints.Private;
using Api.Endpoints.Public;
using Application.Ops;
using Hangfire;
using Infrastructure.Jobs.Fundamentals;
using Infrastructure.Jobs.Market;
using Infrastructure.Jobs.News;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiInfrastructure();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting(); // explícito: debe ir después de static files para que los assets no sean interceptados por el fallback
app.UseApiInfrastructure();
app.UseHttpsRedirection();
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

    var distributionCadenceMinutes = 1440;
    if (!skipStartupDbReads)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            distributionCadenceMinutes = (await scope.ServiceProvider
                .GetRequiredService<IOperationalConfigRepository>()
                .GetAsync()).DistributionCadenceMinutes;
        }
        catch (Exception ex)
        {
            var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
            startupLogger.LogError(ex, "No se pudo leer DistributionCadenceMinutes desde BD al arranque. Usando default.");
        }
    }

    RecurringJob.AddOrUpdate<DistributionPipelineJob>(
        DistributionPipelineSchedule.JobId,
        j => j.ExecuteAsync(CancellationToken.None),
        DistributionPipelineSchedule.GetCronExpression(distributionCadenceMinutes),
        new RecurringJobOptions { TimeZone = mexicoTz });

    var fundamentalsCadenceMinutes = 1440;
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
        j => j.ExecuteAsync(CancellationToken.None),
        FundamentalsPipelineSchedule.GetCronExpression(fundamentalsCadenceMinutes),
        new RecurringJobOptions { TimeZone = mexicoTz });
}

app.Run();

public partial class Program { }
