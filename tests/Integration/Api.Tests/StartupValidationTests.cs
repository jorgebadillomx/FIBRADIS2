using Infrastructure.Persistence.SqlServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Api.Tests;

public class StartupValidationTests
{
    private const string PlaceholderSecret = "CHANGE-ME-IN-PRODUCTION-VIA-ENV-VAR-OR-SECRET-MANAGER!!";

    [Fact]
    public void Startup_WithPlaceholderSecretInNonDevelopment_ThrowsOptionsValidationException()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");

                builder.ConfigureAppConfiguration(config =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:Secret"] = PlaceholderSecret,
                        ["Jwt:Issuer"] = "fibradis",
                        ["Jwt:Audience"] = "fibradis-client",
                        ["Hangfire:UseInMemoryStorage"] = "true",
                    });
                });

                builder.ConfigureServices(services =>
                {
                    var dbRoot = new InMemoryDatabaseRoot();
                    var optionsConfigType = typeof(IDbContextOptionsConfiguration<AppDbContext>);
                    var toRemove = services
                        .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                                 || d.ServiceType == typeof(AppDbContext)
                                 || d.ServiceType == optionsConfigType)
                        .ToList();
                    foreach (var d in toRemove) services.Remove(d);
                    services.AddDbContext<AppDbContext>(o =>
                        o.UseInMemoryDatabase("StartupValidationTest", dbRoot));
                });
            });

        Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
    }
}
