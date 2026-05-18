using Domain.Auth;
using Domain.Catalog;
using Infrastructure.Persistence.SqlServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure; // IDbContextOptionsConfiguration
using Microsoft.EntityFrameworkCore.Storage; // InMemoryDatabaseRoot
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

public class ApiWebFactory : WebApplicationFactory<Program>
{
    // Explicit root per-instance: aísla completamente el store en memoria
    // aunque EF Core InMemory use cachés estáticas por nombre.
    private readonly InMemoryDatabaseRoot _dbRoot = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-secret-key-must-be-at-least-32-chars-long!!!",
                ["Jwt:Issuer"] = "fibradis",
                ["Jwt:Audience"] = "fibradis-client",
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Hangfire:UseInMemoryStorage"] = "true",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remover DbContextOptions, AppDbContext e IDbContextOptionsConfiguration<AppDbContext>
            // para evitar que EF Core aplique tanto la configuración de SqlServer como la de InMemory
            var optionsConfigType = typeof(IDbContextOptionsConfiguration<AppDbContext>);
            var toRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                         || d.ServiceType == typeof(AppDbContext)
                         || d.ServiceType == optionsConfigType)
                .ToList();
            foreach (var d in toRemove) services.Remove(d);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("ApiTests", _dbRoot));
        });
    }

    public async Task SeedUsersAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (!await db.Users.AnyAsync())
        {
            db.Users.AddRange(
                new User
                {
                    Id = Guid.Parse("11111111-0000-0000-0000-000000000001"),
                    Email = "user@test.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    Role = UserRole.User,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                },
                new User
                {
                    Id = Guid.Parse("22222222-0000-0000-0000-000000000001"),
                    Email = "adminops@test.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin456"),
                    Role = UserRole.AdminOps,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                });
            await db.SaveChangesAsync();
        }
    }

    public async Task SeedCatalogAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        // HasData() siembra las FIBRAs activas de producción via EnsureCreatedAsync.
        // Solo agregamos la FIBRA inactiva de test que no está en HasData.
        if (!await db.Fibras.AnyAsync(f => f.Ticker == "INACTIVA1"))
        {
            db.Fibras.Add(new Fibra
            {
                Id = Guid.Parse("cccccccc-0000-0000-0000-000000000001"),
                Ticker = "INACTIVA1",
                FullName = "Fibra Inactiva Test",
                ShortName = "Inactiva",
                Sector = "Diversificado",
                Market = "BMV",
                Currency = "MXN",
                State = FibraState.Inactive,
                NameVariants = [],
                CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }
    }
}
