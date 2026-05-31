using Domain.Ai;
using Domain.Auth;
using Domain.Catalog;
using Domain.Fundamentals;
using Domain.Jobs;
using Domain.Market;
using Domain.News;
using Domain.Ops;
using Infrastructure.Persistence.Seed;
using Infrastructure.Persistence.Seeds;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.SqlServer;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Fibra> Fibras => Set<Fibra>();
    public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();
    public DbSet<DailySnapshot> DailySnapshots => Set<DailySnapshot>();
    public DbSet<Distribution> Distributions => Set<Distribution>();
    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();
    public DbSet<NewsArticleFibra> NewsArticleFibras => Set<NewsArticleFibra>();
    public DbSet<BlocklistTerm> BlocklistTerms => Set<BlocklistTerm>();
    public DbSet<AiModeConfig> AiModeConfigs => Set<AiModeConfig>();
    public DbSet<AiProviderConfig> AiProviderConfigs => Set<AiProviderConfig>();
    public DbSet<AiPrompt> AiPrompts => Set<AiPrompt>();
    public DbSet<PipelineErrorLog> PipelineErrorLogs => Set<PipelineErrorLog>();
    public DbSet<PipelineRunLog> PipelineRunLogs => Set<PipelineRunLog>();
    public DbSet<FundamentalRecord> FundamentalRecords => Set<FundamentalRecord>();
    public DbSet<OperationalConfig> OperationalConfigs => Set<OperationalConfig>();
    public DbSet<EditorialPage> EditorialPages => Set<EditorialPage>();
    public DbSet<ConfigAuditLog> ConfigAuditLogs => Set<ConfigAuditLog>();
    public DbSet<AiCallLog> AiCallLogs => Set<AiCallLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        CatalogSeed.Seed(modelBuilder);
        MarketSeed.Seed(modelBuilder);
        NewsSeed.Seed(modelBuilder);
    }
}
