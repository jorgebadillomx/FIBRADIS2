using Api.Authentication;
using Api.HealthChecks;
using Application.Auth;
using Application.Catalog;
using Application.Market;
using Application.News;
using Hangfire;
using Hangfire.SqlServer;
using Infrastructure.Integrations.Ai;
using Infrastructure.Integrations.Articles;
using Infrastructure.Integrations.GoogleNews;
using Infrastructure.Integrations.OgImage;
using Infrastructure.Integrations.Yahoo;
using YahooQuotesApi;
using Infrastructure.Jobs.Market;
using Infrastructure.Jobs.News;
using Infrastructure.Persistence.Repositories.Catalog;
using Infrastructure.Persistence.Repositories.Market;
using Infrastructure.Persistence.Repositories.News;
using Infrastructure.Persistence.SqlServer;
using Infrastructure.Security;
using Infrastructure.Time;

namespace Api.CompositionRoot;

public static class ApiServiceExtensions
{
    public static WebApplicationBuilder AddApiInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenApi("v1");

        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = ctx =>
            {
                ctx.ProblemDetails.Extensions["correlationId"] =
                    ctx.HttpContext.Items["CorrelationId"]?.ToString()
                    ?? ctx.HttpContext.TraceIdentifier;

                if (!ctx.ProblemDetails.Extensions.ContainsKey("domainCode"))
                    ctx.ProblemDetails.Extensions["domainCode"] = null;
            };
        });

        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddCors(options =>
                options.AddPolicy("SpaDev", policy =>
                    policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials()));
        }

        builder.AddFibradisAuthentication();
        builder.AddFibradisAuthorization();

        builder.Services.AddSingleton<ITokenService, TokenService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IFibraRepository, FibraRepository>();
        builder.Services.AddScoped<IMarketRepository, MarketRepository>();
        builder.Services.AddScoped<MarketPipelineJob>();
        builder.Services.AddScoped<DailySnapshotHistoricalJob>();
        builder.Services.AddScoped<NewsPipelineJob>();
        builder.Services.AddScoped<INewsRepository, NewsRepository>();
        builder.Services.AddScoped<IBlocklistRepository, BlocklistRepository>();
        builder.Services.AddScoped<IAiModeRepository, AiModeRepository>();
        builder.Services.AddSingleton<ITimeService, SystemTimeService>();
        builder.Services.AddSingleton<IBmvSchedule, BmvSchedule>();
        builder.Services.AddSingleton(_ => new YahooQuotesBuilder().Build());
        builder.Services.AddSingleton(
            _ => new YahooQuotesHistory(
                new YahooQuotesBuilder()
                    .WithHistoryStartDate(NodaTime.Instant.FromDateTimeUtc(
                        new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)))
                    .Build()));
        builder.Services.AddSingleton<IYahooFinanceClient, YahooFinanceClient>();
        builder.Services.AddScoped<DistributionPipelineJob>();
        builder.Services.AddHttpClient<IAiSummaryService, GeminiAiSummaryService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        builder.Services.AddHttpClient<IRssClient, GoogleNewsRssClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("FIBRADIS/1.0 (+https://fibradis.mx)");
        });
        builder.Services.AddHttpClient<IGoogleNewsUrlDecoder, GoogleNewsUrlDecoder>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("es-MX,es;q=0.9,en;q=0.8");
        });
        builder.Services.AddHttpClient<IOgImageScraper, OgImageScraper>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("FIBRADIS/1.0 (+https://fibradis.mx)");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5,
        });
        builder.Services.AddHttpClient<IArticleContentScraper, ArticleContentScraper>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("FIBRADIS/1.0 (+https://fibradis.mx)");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5,
        });

        // Hangfire — condicional para soportar tests sin SQL
        var useInMemoryHangfire = builder.Configuration.GetValue<bool>("Hangfire:UseInMemoryStorage");
        var hangfireConnStr = builder.Configuration.GetConnectionString("DefaultConnection");

        if (!useInMemoryHangfire && !string.IsNullOrEmpty(hangfireConnStr))
        {
            builder.Services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(hangfireConnStr, new SqlServerStorageOptions
                {
                    SchemaName = "jobs",
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true,
                }));

            builder.Services.AddHangfireServer(options =>
            {
                options.WorkerCount = 1;
                options.Queues = ["default"];
            });
        }
        else
        {
            // Tests o entornos sin connection string: registro mínimo sin storage ni servidor
            builder.Services.AddHangfire(_ => { });
        }

        // Health checks — siempre registrar
        builder.Services.AddSingleton<PipelineFreshnessHealthCheck>();
        builder.Services.AddSingleton<DatabaseHealthCheck>();
        builder.Services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database")
            .AddCheck<PipelineFreshnessHealthCheck>("pipeline-freshness");

        return builder;
    }
}
