using Domain.Auth;
using Domain.Catalog;
using Domain.Market;
using Domain.News;
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
    public DbSet<BlocklistTerm> BlocklistTerms => Set<BlocklistTerm>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        CatalogSeed.Seed(modelBuilder);
        MarketSeed.Seed(modelBuilder);
        NewsSeed.Seed(modelBuilder);
    }
}
