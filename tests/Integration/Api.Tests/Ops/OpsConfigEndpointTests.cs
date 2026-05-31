using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure.Persistence.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using SharedApiContracts.Auth;
using SharedApiContracts.Ops;

namespace Api.Tests.Ops;

public class OpsConfigEndpointTests(ApiWebFactory factory) : IClassFixture<ApiWebFactory>, IAsyncLifetime
{
    private readonly ApiWebFactory _factory = factory;
    private HttpClient _adminClient = null!;
    private HttpClient _userClient = null!;
    private HttpClient _anonClient = null!;

    public async Task InitializeAsync()
    {
        await _factory.SeedUsersAsync();
        await ResetOperationalConfigAsync();

        _adminClient = _factory.CreateClient();
        _adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await LoginAndGetTokenAsync("adminops@test.com", "ops123"));

        _userClient = _factory.CreateClient();
        _userClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await LoginAndGetTokenAsync("user@test.com", "password123"));

        _anonClient = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetConfig_WithAdminToken_Returns200AndDefaults()
    {
        var response = await _adminClient.GetAsync("/api/v1/ops/config");
        var body = await response.Content.ReadFromJsonAsync<OperationalConfigDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(0.006m, body!.CommissionFactor);
        Assert.Equal(4, body.AvgPeriods);
        Assert.Equal(60, body.NewsCadenceMinutes);
        Assert.Equal(15, body.FibraNewsMonths);
        Assert.Equal(360, body.FundamentalsCadenceMinutes);
    }

    [Fact]
    public async Task GetConfig_WithoutToken_Returns401()
    {
        var response = await _anonClient.GetAsync("/api/v1/ops/config");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetConfig_WithUserRole_Returns403()
    {
        var response = await _userClient.GetAsync("/api/v1/ops/config");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PutConfig_CommissionFactorUpdate_PersistsValue()
    {
        var response = await _adminClient.PutAsJsonAsync(
            "/api/v1/ops/config",
            new UpdateOperationalConfigRequest(0.008m, null, null, null));
        var getResponse = await _adminClient.GetAsync("/api/v1/ops/config");
        var body = await getResponse.Content.ReadFromJsonAsync<OperationalConfigDto>();

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(0.008m, body!.CommissionFactor);
    }

    [Fact]
    public async Task PutConfig_WithAllNullFields_Returns400()
    {
        var response = await _adminClient.PutAsJsonAsync(
            "/api/v1/ops/config",
            new UpdateOperationalConfigRequest(null, null, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutConfig_WithNegativeCommissionFactor_Returns400()
    {
        var response = await _adminClient.PutAsJsonAsync(
            "/api/v1/ops/config",
            new UpdateOperationalConfigRequest(-0.001m, null, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutConfig_WithTooHighCommissionFactor_Returns400()
    {
        var response = await _adminClient.PutAsJsonAsync(
            "/api/v1/ops/config",
            new UpdateOperationalConfigRequest(0.15m, null, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutConfig_WithZeroAvgPeriods_Returns400()
    {
        var response = await _adminClient.PutAsJsonAsync(
            "/api/v1/ops/config",
            new UpdateOperationalConfigRequest(null, 0, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutConfig_WithTooHighAvgPeriods_Returns400()
    {
        var response = await _adminClient.PutAsJsonAsync(
            "/api/v1/ops/config",
            new UpdateOperationalConfigRequest(null, 25, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutConfig_WithInvalidCadence_Returns400()
    {
        var response = await _adminClient.PutAsJsonAsync(
            "/api/v1/ops/config",
            new UpdateOperationalConfigRequest(null, null, 45, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutConfig_WithValidCadence_PersistsValue()
    {
        var response = await _adminClient.PutAsJsonAsync(
            "/api/v1/ops/config",
            new UpdateOperationalConfigRequest(null, null, 30, null));
        var getResponse = await _adminClient.GetAsync("/api/v1/ops/config");
        var body = await getResponse.Content.ReadFromJsonAsync<OperationalConfigDto>();

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(30, body!.NewsCadenceMinutes);
    }

    [Fact]
    public async Task PutConfig_WithValidFundamentalsCadence_PersistsValue()
    {
        var response = await _adminClient.PutAsJsonAsync(
            "/api/v1/ops/config",
            new UpdateOperationalConfigRequest(null, null, null, null, 720));
        var getResponse = await _adminClient.GetAsync("/api/v1/ops/config");
        var body = await getResponse.Content.ReadFromJsonAsync<OperationalConfigDto>();

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(720, body!.FundamentalsCadenceMinutes);
    }

    [Fact]
    public async Task PutConfig_WithoutToken_Returns401()
    {
        var response = await _anonClient.PutAsJsonAsync(
            "/api/v1/ops/config",
            new UpdateOperationalConfigRequest(0.008m, null, null, null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLog_AfterChange_ReturnsEntriesInDescendingOrder()
    {
        await _adminClient.PutAsJsonAsync(
            "/api/v1/ops/config",
            new UpdateOperationalConfigRequest(null, 6, null, null));

        await Task.Delay(10);

        await _adminClient.PutAsJsonAsync(
            "/api/v1/ops/config",
            new UpdateOperationalConfigRequest(0.008m, null, null, null));

        var response = await _adminClient.GetAsync("/api/v1/ops/audit-log");
        var body = await response.Content.ReadFromJsonAsync<List<ConfigAuditLogDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.True(body!.Count >= 2);
        Assert.Contains(body!, entry => entry.FieldName == "avg_periods");
        Assert.Contains(body!, entry => entry.FieldName == "commission_factor");
        Assert.True(body![0].ChangedAt >= body![1].ChangedAt);
    }

    [Fact]
    public async Task GetAuditLog_WithoutToken_Returns401()
    {
        var response = await _anonClient.GetAsync("/api/v1/ops/audit-log");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutConfig_WithUserRole_Returns403()
    {
        var response = await _userClient.PutAsJsonAsync(
            "/api/v1/ops/config",
            new UpdateOperationalConfigRequest(0.008m, null, null, null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLog_WithUserRole_Returns403()
    {
        var response = await _userClient.GetAsync("/api/v1/ops/audit-log");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<string> LoginAndGetTokenAsync(string email, string password)
    {
        var loginResponse = await _factory.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, password));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return loginBody!.AccessToken;
    }

    private async Task ResetOperationalConfigAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        var config = await db.OperationalConfigs.FindAsync(1);
        if (config is null)
        {
            db.OperationalConfigs.Add(new()
            {
                Id = 1,
                CommissionFactor = 0.006m,
                AvgPeriods = 4,
                NewsCadenceMinutes = 60,
                FibraNewsMonths = 15,
                FundamentalsCadenceMinutes = 360,
                UpdatedAt = new DateTimeOffset(2026, 5, 24, 0, 0, 0, TimeSpan.Zero),
                UpdatedBy = "system",
            });
        }
        else
        {
            config.CommissionFactor = 0.006m;
            config.AvgPeriods = 4;
            config.NewsCadenceMinutes = 60;
            config.FibraNewsMonths = 15;
            config.FundamentalsCadenceMinutes = 360;
            config.UpdatedAt = new DateTimeOffset(2026, 5, 24, 0, 0, 0, TimeSpan.Zero);
            config.UpdatedBy = "system";
        }

        db.ConfigAuditLogs.RemoveRange(db.ConfigAuditLogs);
        await db.SaveChangesAsync();
    }
}
