using Api.Authentication;
using Api.HealthChecks;
using Application.Auth;
using Application.Catalog;
using Application.Jobs;
using Application.Market;
using Application.News;
using Application.Ops;
using Hangfire;
using Hangfire.PostgreSql;
using Infrastructure.Integrations.Ai;
using Infrastructure.Integrations.Articles;
using Infrastructure.Integrations.GoogleNews;
using Infrastructure.Integrations.OgImage;
using Infrastructure.Integrations.PdfDiscovery;
using Infrastructure.Integrations.Yahoo;
using YahooQuotesApi;
using Infrastructure.Jobs.Market;
using Infrastructure.Jobs.News;
using Infrastructure.Jobs.Fundamentals;
using Application.Fundamentals;
using Infrastructure.Persistence.Repositories.Catalog;
using Application.Ai;
using Application.Opportunities;
using Application.Portfolio;
using Infrastructure.Persistence.Repositories.Ai;
using Infrastructure.Persistence.Repositories.Fundamentals;
using Infrastructure.Persistence.Repositories.Jobs;
using Infrastructure.Persistence.Repositories.Market;
using Infrastructure.Persistence.Repositories.News;
using Infrastructure.Persistence.Repositories.Ops;
using Infrastructure.Persistence.Repositories.Opportunities;
using Infrastructure.Persistence.Repositories.Portfolio;
using Infrastructure.Persistence.SqlServer;
using Infrastructure.Portfolio;
using Infrastructure.Security;
using Infrastructure.Time;

namespace Api.CompositionRoot;

public static class ApiServiceExtensions
{
    public static WebApplicationBuilder AddApiInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddMemoryCache();
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
        builder.Services.AddSingleton<IEmailEncryptor, EmailEncryptor>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IFibraRepository, FibraRepository>();
        builder.Services.AddScoped<IMarketRepository, MarketRepository>();
        builder.Services.AddScoped<MarketPipelineJob>();
        builder.Services.AddScoped<DailySnapshotHistoricalJob>();
        builder.Services.AddScoped<NewsPipelineJob>();
        builder.Services.AddScoped<NewsBodyTextRetryJob>();
        builder.Services.AddScoped<FundamentalsPipelineJob>();
        builder.Services.AddScoped<INewsRepository, NewsRepository>();
        builder.Services.AddScoped<IBlocklistRepository, BlocklistRepository>();
        builder.Services.AddScoped<IAiModeRepository, AiModeRepository>();
        builder.Services.AddScoped<IOperationalConfigRepository, OperationalConfigRepository>();
        builder.Services.AddScoped<IEditorialPageRepository, EditorialPageRepository>();
        builder.Services.AddScoped<IConfigAuditLogRepository, ConfigAuditLogRepository>();
        builder.Services.AddScoped<IAiPromptRepository, AiPromptRepository>();
        builder.Services.AddScoped<IPipelineErrorLogRepository, PipelineErrorLogRepository>();
        builder.Services.AddScoped<IPipelineRunLogRepository, PipelineRunLogRepository>();
        builder.Services.AddScoped<IFundamentalRepository, FundamentalRepository>();
        builder.Services.AddScoped<IFundamentalSourceManifestRepository, FundamentalSourceManifestRepository>();
        builder.Services.AddScoped<IAiCallLogRepository, AiCallLogRepository>();
        builder.Services.AddScoped<IFundamentalsAutomationService, FundamentalsAutomationService>();
        builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
        builder.Services.AddScoped<IOpportunityWeightsRepository, OpportunityWeightsRepository>();
        builder.Services.AddScoped<IUserFavoritesRepository, UserFavoritesRepository>();
        builder.Services.AddScoped<IPortfolioUploadService, PortfolioUploadService>();
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
        builder.Services.AddScoped<IAiProviderConfigRepository, AiProviderConfigRepository>();
        builder.Services.AddTransient<AiCapturingHandler>();
        builder.Services.AddHttpClient<GeminiKpiExtractorService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(120);
        }).AddHttpMessageHandler<AiCapturingHandler>();
        builder.Services.AddHttpClient<DeepSeekKpiExtractorService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(120);
        }).AddHttpMessageHandler<AiCapturingHandler>();
        builder.Services.AddScoped<IKpiExtractorService, RoutingKpiExtractorService>();
        builder.Services.AddHttpClient<GeminiNewsAnalysisService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        }).AddHttpMessageHandler<AiCapturingHandler>();
        builder.Services.AddHttpClient<DeepSeekNewsAnalysisService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        }).AddHttpMessageHandler<AiCapturingHandler>();
        builder.Services.AddScoped<IAiNewsAnalysisService, RoutingNewsAnalysisService>();
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
        builder.Services.AddHttpClient<IAmefibraDiscoveryClient, AmefibraDiscoveryClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 10,
            UseCookies = true,
        });

        // Discovery sources — multi-source fundamentals pipeline
        builder.Services.AddTransient<AmefibraDiscoverySource>();
        builder.Services.AddTransient<IFundamentalsDiscoverySource>(sp =>
            sp.GetRequiredService<AmefibraDiscoverySource>());
        builder.Services.AddHttpClient<OfficialSiteDiscoverySource>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5,
        });
        builder.Services.AddTransient<IFundamentalsDiscoverySource>(sp =>
            sp.GetRequiredService<OfficialSiteDiscoverySource>());
        builder.Services.AddHttpClient<FHipoWordPressApiDiscoverySource>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5,
            UseCookies = true,
        });
        builder.Services.AddTransient<IFundamentalsDiscoverySource>(sp =>
            sp.GetRequiredService<FHipoWordPressApiDiscoverySource>());
        builder.Services.AddHttpClient<BmvDiscoverySource>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5,
        });
        builder.Services.AddTransient<IFundamentalsDiscoverySource>(sp =>
            sp.GetRequiredService<BmvDiscoverySource>());
        builder.Services.AddHttpClient("FundamentalsDownloader", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 10,
            UseCookies = true,
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
                .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(hangfireConnStr), new PostgreSqlStorageOptions
                {
                    SchemaName = "jobs",
                    InvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.FromSeconds(15),
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
            builder.Services.AddSingleton<IBackgroundJobClient, InMemoryBackgroundJobClient>();
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
