using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Infrastructure.Persistence.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using SharedApiContracts.Auth;
using SharedApiContracts.Portfolio;

namespace Api.Tests;

/// <summary>
/// Tests de integración para endpoints de portafolio (6-1 a 6-4).
/// Cada test crea su propio usuario para evitar contaminación de estado.
/// </summary>
public class PortfolioEndpointTests : IAsyncLifetime
{
    private readonly ApiWebFactory _factory = new();
    private HttpClient _adminClient = null!;
    private HttpClient _anonClient = null!;
    private Guid _funoFibraId;

    public async Task InitializeAsync()
    {
        await _factory.SeedUsersAsync();
        await _factory.SeedCatalogAsync();
        await _factory.SeedMarketAsync();

        // Cliente admin para crear usuarios en tests que lo necesiten
        _adminClient = _factory.CreateClient();
        _adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await LoginAndGetTokenAsync("adminops@test.com", "ops123"));

        _anonClient = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var funo = db.Fibras.FirstOrDefault(f => f.Ticker == "FUNO11");
        _funoFibraId = funo?.Id ?? Guid.Empty;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── GET /api/v1/portfolio/status ──────────────────────────────────────────

    [Fact]
    public async Task GetStatus_WithoutPortfolio_ReturnsHasPortfolioFalse()
    {
        var client = await CreateFreshUserClientAsync();

        var response = await client.GetAsync("/api/v1/portfolio/status");
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(body.GetProperty("hasPortfolio").GetBoolean());
        Assert.Equal(0, body.GetProperty("positionCount").GetInt32());
    }

    [Fact]
    public async Task GetStatus_WithoutToken_Returns401()
    {
        var response = await _anonClient.GetAsync("/api/v1/portfolio/status");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── POST /api/v1/portfolio/upload ─────────────────────────────────────────

    [Fact]
    public async Task Upload_ValidCsv_Returns200WithPositionCount()
    {
        var client = await CreateFreshUserClientAsync();
        var csv = "Ticker,Qty,AvgCost\nFUNO11,100,24.50\n";

        var response = await client.PostAsync("/api/v1/portfolio/upload", BuildCsvUpload(csv, "portfolio.csv"));
        var body = await response.Content.ReadFromJsonAsync<PortfolioUploadResponseDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(1, body!.PositionCount);
    }

    [Fact]
    public async Task Upload_ValidCsv_StatusReflectsPortfolioExists()
    {
        var client = await CreateFreshUserClientAsync();
        var csv = "Ticker,Qty,AvgCost\nFUNO11,50,25.00\n";
        await client.PostAsync("/api/v1/portfolio/upload", BuildCsvUpload(csv, "p.csv"));

        var status = await client.GetAsync("/api/v1/portfolio/status");
        var body = await status.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();

        Assert.True(body.GetProperty("hasPortfolio").GetBoolean());
        Assert.True(body.GetProperty("positionCount").GetInt32() > 0);
    }

    [Fact]
    public async Task Upload_InvalidTicker_Returns400WithErrors()
    {
        var client = await CreateFreshUserClientAsync();
        var csv = "Ticker,Qty,AvgCost\nTICKERINEXISTENTE,100,10.00\n";

        var response = await client.PostAsync("/api/v1/portfolio/upload", BuildCsvUpload(csv, "bad.csv"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_EmptyFileOnlyHeaders_Returns400()
    {
        var client = await CreateFreshUserClientAsync();
        var csv = "Ticker,Qty,AvgCost\n";

        var response = await client.PostAsync("/api/v1/portfolio/upload", BuildCsvUpload(csv, "empty.csv"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WithoutToken_Returns401()
    {
        var csv = "Ticker,Qty,AvgCost\nFUNO11,100,24.50\n";
        var response = await _anonClient.PostAsync("/api/v1/portfolio/upload", BuildCsvUpload(csv, "portfolio.csv"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Upload_MergeMode_CombinesWithExistingPortfolio()
    {
        var client = await CreateFreshUserClientAsync();
        var csv1 = "Ticker,Qty,AvgCost\nFUNO11,100,24.50\n";
        await client.PostAsync("/api/v1/portfolio/upload", BuildCsvUpload(csv1, "p1.csv"));

        var csv2 = "Ticker,Qty,AvgCost\nFUNO11,100,20.00\n";
        var mergeResponse = await client.PostAsync(
            "/api/v1/portfolio/upload?mode=merge&force=true",
            BuildCsvUpload(csv2, "p2.csv"));
        var body = await mergeResponse.Content.ReadFromJsonAsync<PortfolioUploadResponseDto>();

        Assert.Equal(HttpStatusCode.OK, mergeResponse.StatusCode);
        Assert.NotNull(body);
    }

    // ── GET /api/v1/portfolio/ ────────────────────────────────────────────────

    [Fact]
    public async Task GetPortfolio_EmptyPortfolio_ReturnsNullKpisAndEmptyPositions()
    {
        var client = await CreateFreshUserClientAsync();

        var response = await client.GetAsync("/api/v1/portfolio/");
        var body = await response.Content.ReadFromJsonAsync<PortfolioResponseDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Empty(body!.Positions);
        Assert.Null(body.Kpis);
    }

    [Fact]
    public async Task GetPortfolio_WithPositions_ReturnsKpisAndPositions()
    {
        var client = await CreateFreshUserClientAsync();
        var csv = "Ticker,Qty,AvgCost\nFUNO11,100,24.50\n";
        await client.PostAsync("/api/v1/portfolio/upload", BuildCsvUpload(csv, "p.csv"));

        var response = await client.GetAsync("/api/v1/portfolio/");
        var body = await response.Content.ReadFromJsonAsync<PortfolioResponseDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.NotEmpty(body!.Positions);
        Assert.NotNull(body.Kpis);
        Assert.True(body.Kpis!.InversionTotal > 0);
        Assert.Equal("FUNO11", body.Positions[0].Ticker);
        Assert.Equal(100, body.Positions[0].Titulos);
    }

    [Fact]
    public async Task GetPortfolio_WithoutToken_Returns401()
    {
        var response = await _anonClient.GetAsync("/api/v1/portfolio/");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── PATCH /api/v1/portfolio/positions/{fibraId} ──────────────────────────

    [Fact]
    public async Task PatchPosition_ValidUpdate_Returns204()
    {
        var client = await CreateFreshUserClientAsync();
        var csv = "Ticker,Qty,AvgCost\nFUNO11,100,24.50\n";
        await client.PostAsync("/api/v1/portfolio/upload", BuildCsvUpload(csv, "p.csv"));

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/portfolio/positions/{_funoFibraId}",
            new PortfolioPositionPatchDto(200, 23.00m));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PatchPosition_AfterUpdate_ReflectsNewValues()
    {
        var client = await CreateFreshUserClientAsync();
        var csv = "Ticker,Qty,AvgCost\nFUNO11,100,24.50\n";
        await client.PostAsync("/api/v1/portfolio/upload", BuildCsvUpload(csv, "p.csv"));

        await client.PatchAsJsonAsync(
            $"/api/v1/portfolio/positions/{_funoFibraId}",
            new PortfolioPositionPatchDto(200, 22.00m));

        var portfolio = await client.GetAsync("/api/v1/portfolio/");
        var body = await portfolio.Content.ReadFromJsonAsync<PortfolioResponseDto>();
        var position = body!.Positions.FirstOrDefault(p => p.FibraId == _funoFibraId);

        Assert.NotNull(position);
        Assert.Equal(200, position!.Titulos);
        Assert.Equal(22.00m, position.CostoPromedio);
    }

    [Fact]
    public async Task PatchPosition_ZeroTitulos_Returns400()
    {
        var client = await CreateFreshUserClientAsync();
        var csv = "Ticker,Qty,AvgCost\nFUNO11,100,24.50\n";
        await client.PostAsync("/api/v1/portfolio/upload", BuildCsvUpload(csv, "p.csv"));

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/portfolio/positions/{_funoFibraId}",
            new PortfolioPositionPatchDto(0, 24.50m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PatchPosition_NonExistingFibra_Returns404()
    {
        var client = await CreateFreshUserClientAsync();

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/portfolio/positions/{Guid.NewGuid()}",
            new PortfolioPositionPatchDto(100, 24.50m));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PatchPosition_WithoutToken_Returns401()
    {
        var response = await _anonClient.PatchAsJsonAsync(
            $"/api/v1/portfolio/positions/{_funoFibraId}",
            new PortfolioPositionPatchDto(100, 24.50m));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── DELETE /api/v1/portfolio/positions/{fibraId} ─────────────────────────

    [Fact]
    public async Task DeletePosition_ExistingPosition_Returns204()
    {
        var client = await CreateFreshUserClientAsync();
        var csv = "Ticker,Qty,AvgCost\nFUNO11,100,24.50\n";
        await client.PostAsync("/api/v1/portfolio/upload", BuildCsvUpload(csv, "p.csv"));

        var response = await client.DeleteAsync($"/api/v1/portfolio/positions/{_funoFibraId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeletePosition_AfterDelete_PortfolioIsEmpty()
    {
        var client = await CreateFreshUserClientAsync();
        var csv = "Ticker,Qty,AvgCost\nFUNO11,100,24.50\n";
        await client.PostAsync("/api/v1/portfolio/upload", BuildCsvUpload(csv, "p.csv"));

        await client.DeleteAsync($"/api/v1/portfolio/positions/{_funoFibraId}");

        var status = await client.GetAsync("/api/v1/portfolio/status");
        var body = await status.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();

        Assert.False(body.GetProperty("hasPortfolio").GetBoolean());
    }

    [Fact]
    public async Task DeletePosition_NonExisting_Returns404()
    {
        var client = await CreateFreshUserClientAsync();

        var response = await client.DeleteAsync($"/api/v1/portfolio/positions/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeletePosition_WithoutToken_Returns401()
    {
        var response = await _anonClient.DeleteAsync($"/api/v1/portfolio/positions/{_funoFibraId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── GET/PUT /api/v1/portfolio/column-config ───────────────────────────────

    [Fact]
    public async Task GetColumnConfig_ReturnsEmptyByDefault()
    {
        var client = await CreateFreshUserClientAsync();

        var response = await client.GetAsync("/api/v1/portfolio/column-config");
        var body = await response.Content.ReadFromJsonAsync<PortfolioColumnConfigDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Empty(body!.Columns);
    }

    [Fact]
    public async Task PutColumnConfig_ValidColumns_PersistsAndReturns204()
    {
        var client = await CreateFreshUserClientAsync();
        var request = new PortfolioColumnConfigDto(["capRate", "navPerCbfi"]);

        var putResponse = await client.PutAsJsonAsync("/api/v1/portfolio/column-config", request);
        var getResponse = await client.GetAsync("/api/v1/portfolio/column-config");
        var body = await getResponse.Content.ReadFromJsonAsync<PortfolioColumnConfigDto>();

        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);
        Assert.Contains("capRate", body!.Columns);
        Assert.Contains("navPerCbfi", body.Columns);
    }

    [Fact]
    public async Task PutColumnConfig_InvalidColumns_FilteredOut()
    {
        var client = await CreateFreshUserClientAsync();
        var request = new PortfolioColumnConfigDto(["capRate", "columnaInvalida"]);

        await client.PutAsJsonAsync("/api/v1/portfolio/column-config", request);
        var getResponse = await client.GetAsync("/api/v1/portfolio/column-config");
        var body = await getResponse.Content.ReadFromJsonAsync<PortfolioColumnConfigDto>();

        Assert.Contains("capRate", body!.Columns);
        Assert.DoesNotContain("columnaInvalida", body.Columns);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<string> LoginAndGetTokenAsync(string email, string password)
    {
        var response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/login", new LoginRequest(email, password));
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.AccessToken;
    }

    /// <summary>Crea un usuario con email único por test para garantizar aislamiento de portafolio.</summary>
    private async Task<HttpClient> CreateFreshUserClientAsync()
    {
        var email = $"user-{Guid.NewGuid():N}@test.com";
        await _adminClient.PostAsJsonAsync("/api/v1/ops/users",
            new CreateUserRequest(email, "Fuerte1!", "User", null, null));

        var token = await LoginAndGetTokenAsync(email, "Fuerte1!");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static MultipartFormDataContent BuildCsvUpload(string csvContent, string fileName)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csvContent));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        content.Add(fileContent, "file", fileName);
        return content;
    }
}
