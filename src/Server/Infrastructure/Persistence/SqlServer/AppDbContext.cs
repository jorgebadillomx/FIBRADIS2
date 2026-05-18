using Domain.Auth;
using Domain.Catalog;
using Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.SqlServer;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Fibra> Fibras => Set<Fibra>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        CatalogSeed.Seed(modelBuilder);
    }
}
