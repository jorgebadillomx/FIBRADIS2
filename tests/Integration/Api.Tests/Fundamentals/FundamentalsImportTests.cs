using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Domain.Catalog;
using Domain.Fundamentals;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharedApiContracts.Auth;
using SharedApiContracts.Fundamentals;

namespace Api.Tests.Fundamentals;

public class FundamentalsImportTests(ApiWebFactory factory) : IClassFixture<ApiWebFactory>, IAsyncLifetime
{
    private readonly ApiWebFactory _factory = factory;
    private HttpClient _adminClient = null!;
    private HttpClient _userClient = null!;
    private HttpClient _anonClient = null!;
    private Guid _funoId;

    public async Task InitializeAsync()
    {
        await _factory.SeedUsersAsync();
        await _factory.SeedCatalogAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        var fibra = await db.Set<Fibra>().FirstAsync(f => f.Ticker == "FUNO11");
        _funoId = fibra.Id;

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
    public async Task Import_WithCompletePayload_Returns200_StatusPending()
    {
        var payload = new ImportFundamentalsRequest(
            FibraId: _funoId,
            Period: "Q3-2024",
            CapRate: 0.08m,
            NavPerCbfi: 120m,
            Ltv: 0.35m,
            NoiMargin: 0.72m,
            FfoMargin: 0.65m,
            QuarterlyDistribution: 0.45m,
            Summary: "Buen trimestre",
            PdfReference: null);

        var response = await _adminClient.PostAsJsonAsync("/api/v1/ops/fundamentals/import", payload);
        var body = await response.Content.ReadFromJsonAsync<FundamentalPreviewDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("pending", body!.Status);
        Assert.Equal("FUNO11", body.FibraTicker);
        Assert.Equal("Q3-2024", body.Period);
        Assert.False(body.IsPossibleUpdate);
        Assert.Equal(6, body.PresentFields.Count);
        Assert.Empty(body.MissingFields);
    }

    [Fact]
    public async Task Import_WithPartialPayload_Returns200_StatusPartial()
    {
        var payload = new ImportFundamentalsRequest(
            FibraId: _funoId,
            Period: "Q1-2024",
            CapRate: 0.08m,
            NavPerCbfi: null,
            Ltv: null,
            NoiMargin: null,
            FfoMargin: null,
            QuarterlyDistribution: null,
            Summary: null,
            PdfReference: null);

        var response = await _adminClient.PostAsJsonAsync("/api/v1/ops/fundamentals/import", payload);
        var body = await response.Content.ReadFromJsonAsync<FundamentalPreviewDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("partial", body!.Status);
        Assert.Single(body.PresentFields);
        Assert.Equal(5, body.MissingFields.Count);
    }

    [Fact]
    public async Task Import_ForAlreadyProcessedPeriod_Returns200_IsPossibleUpdate()
    {
        var payload = new ImportFundamentalsRequest(
            FibraId: _funoId,
            Period: "Q4-2023",
            CapRate: 0.07m,
            NavPerCbfi: 115m,
            Ltv: 0.3m,
            NoiMargin: 0.7m,
            FfoMargin: 0.6m,
            QuarterlyDistribution: 0.4m,
            Summary: null,
            PdfReference: null);

        var first = await _adminClient.PostAsJsonAsync("/api/v1/ops/fundamentals/import", payload);
        var preview = await first.Content.ReadFromJsonAsync<FundamentalPreviewDto>();
        await _adminClient.PostAsJsonAsync($"/api/v1/ops/fundamentals/{preview!.Id}/confirm", (object?)null);

        var second = await _adminClient.PostAsJsonAsync("/api/v1/ops/fundamentals/import", payload);
        var body = await second.Content.ReadFromJsonAsync<FundamentalPreviewDto>();

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.True(body!.IsPossibleUpdate);
        Assert.NotNull(body.WarningMessage);
    }

    [Fact]
    public async Task Import_WithoutToken_Returns401()
    {
        var payload = new ImportFundamentalsRequest(
            FibraId: _funoId, Period: "Q3-2024",
            CapRate: 0.08m, NavPerCbfi: null, Ltv: null,
            NoiMargin: null, FfoMargin: null, QuarterlyDistribution: null,
            Summary: null, PdfReference: null);

        var response = await _anonClient.PostAsJsonAsync("/api/v1/ops/fundamentals/import", payload);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Confirm_Returns200_StatusProcessed()
    {
        var payload = new ImportFundamentalsRequest(
            FibraId: _funoId, Period: "Q2-2024",
            CapRate: 0.08m, NavPerCbfi: 120m, Ltv: 0.35m,
            NoiMargin: 0.72m, FfoMargin: 0.65m, QuarterlyDistribution: 0.45m,
            Summary: null, PdfReference: null);

        var importResponse = await _adminClient.PostAsJsonAsync("/api/v1/ops/fundamentals/import", payload);
        var preview = await importResponse.Content.ReadFromJsonAsync<FundamentalPreviewDto>();

        var confirmResponse = await _adminClient.PostAsJsonAsync($"/api/v1/ops/fundamentals/{preview!.Id}/confirm", (object?)null);
        var body = await confirmResponse.Content.ReadFromJsonAsync<FundamentalRecordDto>();

        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        Assert.Equal("processed", body!.Status);
        Assert.Equal("adminops@test.com", body.ConfirmedBy);
    }

    [Fact]
    public async Task Confirm_WithNonExistentId_Returns404()
    {
        var response = await _adminClient.PostAsJsonAsync($"/api/v1/ops/fundamentals/{Guid.NewGuid()}/confirm", (object?)null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetByFibra_Returns200_WithList()
    {
        var payload = new ImportFundamentalsRequest(
            FibraId: _funoId, Period: "Q3-2023",
            CapRate: 0.08m, NavPerCbfi: null, Ltv: null,
            NoiMargin: null, FfoMargin: null, QuarterlyDistribution: null,
            Summary: null, PdfReference: null);
        await _adminClient.PostAsJsonAsync("/api/v1/ops/fundamentals/import", payload);

        var response = await _adminClient.GetAsync($"/api/v1/ops/fundamentals?fibraId={_funoId}");
        var body = await response.Content.ReadFromJsonAsync<List<FundamentalRecordDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.True(body!.Count >= 1);
    }

    [Fact]
    public async Task GetByFibra_WithoutToken_Returns401()
    {
        var response = await _anonClient.GetAsync($"/api/v1/ops/fundamentals?fibraId={_funoId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPublicLatest_WithNoProcessedRecord_Returns404()
    {
        var response = await _anonClient.GetAsync("/api/v1/fundamentals/FICERT1/latest");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPublicLatest_WithNonExistentTicker_Returns404()
    {
        var response = await _anonClient.GetAsync("/api/v1/fundamentals/NOEXISTE/latest");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPublicLatest_WithProcessedRecord_Returns200_FundamentalesPublicDto()
    {
        var payload = new ImportFundamentalsRequest(
            FibraId: _funoId, Period: "Q4-2024",
            CapRate: 0.09m, NavPerCbfi: 125m, Ltv: 0.32m,
            NoiMargin: 0.74m, FfoMargin: 0.67m, QuarterlyDistribution: 0.48m,
            Summary: "Resumen público", PdfReference: null);

        var importResponse = await _adminClient.PostAsJsonAsync("/api/v1/ops/fundamentals/import", payload);
        var preview = await importResponse.Content.ReadFromJsonAsync<FundamentalPreviewDto>();
        await _adminClient.PostAsJsonAsync($"/api/v1/ops/fundamentals/{preview!.Id}/confirm", (object?)null);

        var response = await _anonClient.GetAsync("/api/v1/fundamentals/FUNO11/latest?period=Q4-2024");
        var body = await response.Content.ReadFromJsonAsync<FundamentalesPublicDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("Q4-2024", body!.Period);
        Assert.Equal(0.09m, body.CapRate);
    }

    [Fact]
    public async Task GetPublicLatest_IsAccessibleAnonymously()
    {
        var response = await _anonClient.GetAsync("/api/v1/fundamentals/FUNO11/latest");
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Import_WithUserRole_Returns403()
    {
        var payload = new ImportFundamentalsRequest(
            FibraId: _funoId, Period: "Q3-2024",
            CapRate: 0.08m, NavPerCbfi: null, Ltv: null,
            NoiMargin: null, FfoMargin: null, QuarterlyDistribution: null,
            Summary: null, PdfReference: null);

        var response = await _userClient.PostAsJsonAsync("/api/v1/ops/fundamentals/import", payload);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetByFibra_WithUserRole_Returns403()
    {
        var response = await _userClient.GetAsync($"/api/v1/ops/fundamentals?fibraId={_funoId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PatchFieldNotes_Returns200_AndReplacesNotes()
    {
        var payload = new ImportFundamentalsRequest(
            FibraId: _funoId, Period: "Q1-2025",
            CapRate: 0.08m, NavPerCbfi: 120m, Ltv: 0.35m,
            NoiMargin: 0.72m, FfoMargin: 0.65m, QuarterlyDistribution: 0.45m,
            Summary: "Resumen", PdfReference: null);

        var importResponse = await _adminClient.PostAsJsonAsync("/api/v1/ops/fundamentals/import", payload);
        var preview = await importResponse.Content.ReadFromJsonAsync<FundamentalPreviewDto>();
        await _adminClient.PostAsJsonAsync($"/api/v1/ops/fundamentals/{preview!.Id}/confirm", (object?)null);

        var response = await _adminClient.PatchAsJsonAsync($"/api/v1/ops/fundamentals/{preview.Id}/field-notes", new PatchFieldNotesRequest(
            CapRateNote: "Cap rate validado por Ops",
            NavPerCbfiNote: "NAV recalculado",
            LtvNote: null,
            NoiMarginNote: "NOI revisado",
            FfoMarginNote: null,
            QuarterlyDistributionNote: "Distribución confirmada"));
        var body = await response.Content.ReadFromJsonAsync<FundamentalRecordDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("Cap rate validado por Ops", body!.FieldNotes!["capRate"]);
        Assert.Equal("NAV recalculado", body.FieldNotes["navPerCbfi"]);
        Assert.False(body.FieldNotes.ContainsKey("ltv"));
        Assert.Equal("Distribución confirmada", body.FieldNotes["quarterlyDistribution"]);
    }

    [Fact]
    public async Task PatchFieldNotes_WithNonExistentRecord_Returns404()
    {
        var response = await _adminClient.PatchAsJsonAsync($"/api/v1/ops/fundamentals/{Guid.NewGuid()}/field-notes", new PatchFieldNotesRequest(
            CapRateNote: "nota",
            NavPerCbfiNote: null,
            LtvNote: null,
            NoiMarginNote: null,
            FfoMarginNote: null,
            QuarterlyDistributionNote: null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PatchFieldNotes_WithoutToken_Returns401()
    {
        var response = await _anonClient.PatchAsJsonAsync($"/api/v1/ops/fundamentals/{Guid.NewGuid()}/field-notes", new PatchFieldNotesRequest(
            CapRateNote: "nota",
            NavPerCbfiNote: null,
            LtvNote: null,
            NoiMarginNote: null,
            FfoMarginNote: null,
            QuarterlyDistributionNote: null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PatchFieldNotes_WithUserRole_Returns403()
    {
        var response = await _userClient.PatchAsJsonAsync($"/api/v1/ops/fundamentals/{Guid.NewGuid()}/field-notes", new PatchFieldNotesRequest(
            CapRateNote: "nota",
            NavPerCbfiNote: null,
            LtvNote: null,
            NoiMarginNote: null,
            FfoMarginNote: null,
            QuarterlyDistributionNote: null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPublicLatest_ReturnsEnrichedAiFields_WithArraysNeverNull()
    {
        var payload = new ImportFundamentalsRequest(
            FibraId: _funoId, Period: "Q2-2025",
            CapRate: 0.09m, NavPerCbfi: 125m, Ltv: 0.32m,
            NoiMargin: 0.74m, FfoMargin: 0.67m, QuarterlyDistribution: 0.48m,
            Summary: "Resumen público", PdfReference: null);

        var importResponse = await _adminClient.PostAsJsonAsync("/api/v1/ops/fundamentals/import", payload);
        var preview = await importResponse.Content.ReadFromJsonAsync<FundamentalPreviewDto>();
        await _adminClient.PostAsJsonAsync($"/api/v1/ops/fundamentals/{preview!.Id}/confirm", (object?)null);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var record = await db.FundamentalRecords.FirstAsync(r => r.Id == preview.Id);
            record.SetAiAnalysis(new FundamentalAiAnalysis(
                SummaryMarkdown: "**Fortaleza** operativa.",
                InvestorTakeaway: "Distribución defendible.",
                OperationalSignals: ["Ocupación sólida"],
                FinancialSignals: [],
                RiskFlags: ["Mayor costo financiero"],
                ExtractionNotes: "Análisis enriquecido"));
            await db.SaveChangesAsync();
        }

        var response = await _anonClient.GetAsync("/api/v1/fundamentals/FUNO11/latest?period=Q2-2025");
        var body = await response.Content.ReadFromJsonAsync<FundamentalesPublicDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("**Fortaleza** operativa.", body!.SummaryMarkdown);
        Assert.Equal("Distribución defendible.", body.InvestorTakeaway);
        Assert.Equal(["Ocupación sólida"], body.OperationalSignals);
        Assert.NotNull(body.FinancialSignals);
        Assert.Empty(body.FinancialSignals);
        Assert.Equal(["Mayor costo financiero"], body.RiskFlags);
    }

    private async Task<string> LoginAndGetTokenAsync(string email, string password)
    {
        var loginResponse = await _factory.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, password));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return loginBody!.AccessToken;
    }
}
