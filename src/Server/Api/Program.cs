using Api.CompositionRoot;
using Api.Endpoints.Ops;
using Api.Endpoints.Private;
using Api.Endpoints.Public;
using Application.Ops;
using Hangfire;
using Infrastructure.Jobs.Market;
using Infrastructure.Jobs.News;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiInfrastructure();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.UseApiInfrastructure();
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
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

app.MapFallback("/api/{**path}", () => Results.NotFound());
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

            var dynCron = opConfig.NewsCadenceMinutes == 60
                ? "0 * * * *"
                : $"*/{opConfig.NewsCadenceMinutes} * * * *";
            RecurringJob.AddOrUpdate<NewsPipelineJob>(
                NewsPipelineSchedule.HourlyJobId,
                j => j.ExecuteAsync(CancellationToken.None),
                dynCron,
                new RecurringJobOptions { TimeZone = mexicoTz });
        }
        catch (Exception ex)
        {
            var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
            startupLogger.LogError(ex, "No se pudo leer NewsCadenceMinutes desde BD al arranque. Usando default.");
        }
    }

    RecurringJob.AddOrUpdate<DistributionPipelineJob>(
        "distribution-pipeline",
        j => j.ExecuteAsync(CancellationToken.None),
        "0 6 * * *",
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
}

app.Run();

public partial class Program { }
