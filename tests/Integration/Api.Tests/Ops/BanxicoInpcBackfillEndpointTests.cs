using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.Integrations;
using Domain.Ops;
using Infrastructure.Persistence.SqlServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SharedApiContracts.Auth;

namespace Api.Tests;

public class BanxicoInpcBackfillEndpointTests
{
    [Fact]
    public async Task Backfill_RequiresAdminOps()
    {
        using var factory = CreateFactory();
        await factory.SeedUsersAsync();

        var anonClient = factory.CreateClient();
        var userClient = await CreateAuthenticatedClientAsync(factory, "user@test.com", "password123");

        var anonResponse = await anonClient.PostAsync("/api/v1/ops/banxico/sync-inpc/backfill", null);
        var userResponse = await userClient.PostAsync("/api/v1/ops/banxico/sync-inpc/backfill", null);

        Assert.Equal(HttpStatusCode.Unauthorized, anonResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, userResponse.StatusCode);
    }

    [Fact]
    public async Task Backfill_WithAdminOpsToken_UsesFixedRange_UpsertsRows_AndLogsRun()
    {
        var fakeBanxico = new FakeBanxicoClient();
        using var factory = CreateFactory(fakeBanxico);
        await factory.SeedUsersAsync();
        await SeedExistingInpcAsync(factory);

        var adminClient = await CreateAuthenticatedClientAsync(factory, "adminops@test.com", "ops123");
        var expectedFrom = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-72);
        var expectedTo = DateOnly.FromDateTime(DateTime.UtcNow);

        var response = await adminClient.PostAsync("/api/v1/ops/banxico/sync-inpc/backfill", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<BackfillResponseDto>();
        Assert.NotNull(body);
        Assert.Equal(expectedFrom.ToString("yyyy-MM"), body!.From);
        Assert.Equal(expectedTo.ToString("yyyy-MM"), body.To);
        Assert.Equal(2, body.Processed);
        Assert.Equal(expectedFrom, fakeBanxico.LastFrom);
        Assert.Equal(expectedTo, fakeBanxico.LastTo);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var run = await db.PipelineRunLogs.SingleAsync(log => log.Pipeline == "BanxicoInpcBackfill");
        Assert.Equal("Completed", run.Status);
        Assert.Equal(2, run.ItemsProcessed);
        Assert.Equal(0, run.ErrorCount);
        Assert.NotNull(run.CompletedAt);
        Assert.Contains("processed", run.Details ?? string.Empty);
    }

    private static BackfillApiWebFactory CreateFactory(FakeBanxicoClient? fakeBanxico = null)
        => new(fakeBanxico ?? new FakeBanxicoClient());

    private static async Task<HttpClient> CreateAuthenticatedClientAsync(ApiWebFactory factory, string email, string password)
    {
        var loginClient = factory.CreateClient();
        var loginResponse = await loginClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.AccessToken);
        return client;
    }

    private static async Task SeedExistingInpcAsync(ApiWebFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        db.InpcMonthlyEntries.Add(new InpcMonthlyEntry
        {
            Periodo = DateOnly.FromDateTime(DateTime.UtcNow),
            InpcIndex = 999m,
            CapturedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    private sealed class BackfillApiWebFactory(FakeBanxicoClient fakeBanxico) : ApiWebFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBanxicoClient>();
                services.AddSingleton<IBanxicoClient>(fakeBanxico);
            });
        }
    }

    private sealed class FakeBanxicoClient : IBanxicoClient
    {
        public DateOnly? LastFrom { get; private set; }
        public DateOnly? LastTo { get; private set; }

        public Task<decimal?> GetCetes28dAsync(CancellationToken ct = default) => Task.FromResult<decimal?>(null);
        public Task<decimal?> GetTiie28dAsync(CancellationToken ct = default) => Task.FromResult<decimal?>(null);

        public Task<IReadOnlyList<(DateOnly Periodo, decimal InpcIndex)>> GetInpcHistoryAsync(
            DateOnly from,
            DateOnly to,
            CancellationToken ct = default)
        {
            LastFrom = from;
            LastTo = to;
            return Task.FromResult<IReadOnlyList<(DateOnly Periodo, decimal InpcIndex)>>([
                (from, 100m),
                (from.AddMonths(1), 101m),
            ]);
        }
    }

    private sealed record BackfillResponseDto(string From, string To, int Processed);
}
