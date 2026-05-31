using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Application.Fundamentals;
using Domain.Catalog;
using Infrastructure.Persistence.SqlServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SharedApiContracts.Auth;
using SharedApiContracts.Fundamentals;

namespace Api.Tests.Fundamentals;

public class FundamentalsExtractKpisTests : IClassFixture<ApiWebFactory>, IAsyncLifetime
{
    private readonly ApiWebFactory _factory;
    private HttpClient _adminClient = null!;
    private HttpClient _userClient = null!;
    private HttpClient _anonClient = null!;
    private Guid _funoId;

    public FundamentalsExtractKpisTests(ApiWebFactory factory)
    {
        _factory = factory;
    }

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
    public async Task ExtractKpis_WithoutAuth_Returns401()
    {
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(CreateTextPdfBytes("Texto")), "file", "reporte.pdf");
        form.Headers.ContentType!.MediaType = "multipart/form-data";
        form.First().Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        var response = await _anonClient.PostAsync("/api/v1/ops/fundamentals/extract-kpis", form);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExtractKpis_WithUserRole_Returns403()
    {
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(CreateTextPdfBytes("Texto")), "file", "reporte.pdf");
        form.First().Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        var response = await _userClient.PostAsync("/api/v1/ops/fundamentals/extract-kpis", form);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ExtractKpis_WithNonPdfFile_Returns400()
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("hola"), "file", "reporte.txt");
        form.First().Headers.ContentType = new MediaTypeHeaderValue("text/plain");

        var response = await _adminClient.PostAsync("/api/v1/ops/fundamentals/extract-kpis", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ExtractKpis_WithPdfWithoutText_Returns200_AllFieldsNullAndOcrNote()
    {
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(CreateBlankPdfBytes()), "file", "scan.pdf");
        form.First().Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        var response = await _adminClient.PostAsync("/api/v1/ops/fundamentals/extract-kpis", form);
        var body = await response.Content.ReadFromJsonAsync<KpiExtractionDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Null(body!.CapRate);
        Assert.Null(body.NavPerCbfi);
        Assert.Null(body.Ltv);
        Assert.Null(body.NoiMargin);
        Assert.Null(body.FfoMargin);
        Assert.Null(body.QuarterlyDistribution);
        Assert.Null(body.Summary);
        Assert.Equal(0, body.MarkdownLength);
        Assert.Contains("sin texto extraíble", body.ExtractionNotes, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExtractKpis_WithPdfText_Returns200_AndUsesExtractorService()
    {
        var stub = new StubKpiExtractorService(new KpiExtractionResult(
            0.0812m,
            "Cap rate calculado desde NOI anualizado y propiedades de inversión.",
            18.50m,
            "NAV por CBFI calculado con patrimonio total y CBFIs en circulación.",
            0.45m,
            "LTV calculado con deuda total / propiedades de inversión.",
            null,
            null,
            null,
            null,
            0.52m,
            "Distribución trimestral declarada vs período previo.",
            "Resumen analítico de prueba.",
            "Se encontraron 4 campos y resumen.",
            true));

        await using var factory = new FundamentalsExtractApiWebFactory(stub);
        await factory.SeedUsersAsync();

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await LoginAndGetTokenAsync(factory, "adminops@test.com", "ops123"));

        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(CreateTextPdfBytes("FUNO11 reportó Cap Rate 8.12% y NAV por CBFI 18.50.")), "file", "reporte.pdf");
        form.First().Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        var response = await client.PostAsync("/api/v1/ops/fundamentals/extract-kpis", form);
        var body = await response.Content.ReadFromJsonAsync<KpiExtractionDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(0.0812m, body!.CapRate);
        Assert.Equal(18.50m, body.NavPerCbfi);
        Assert.Equal(0.45m, body.Ltv);
        Assert.Equal(0.52m, body.QuarterlyDistribution);
        Assert.Equal("Cap rate calculado desde NOI anualizado y propiedades de inversión.", body.CapRateNote);
        Assert.Equal("NAV por CBFI calculado con patrimonio total y CBFIs en circulación.", body.NavPerCbfiNote);
        Assert.Equal("LTV calculado con deuda total / propiedades de inversión.", body.LtvNote);
        Assert.Equal("Distribución trimestral declarada vs período previo.", body.QuarterlyDistributionNote);
        Assert.Equal("Resumen analítico de prueba.", body.Summary);
        Assert.True(body.MarkdownLength > 0);
        Assert.NotNull(stub.LastMarkdownContent);
        Assert.Contains("FUNO11", stub.LastMarkdownContent);
    }

    [Fact]
    public async Task Import_WithAllNumericFieldsNull_Returns200_StatusPartial()
    {
        var payload = new ImportFundamentalsRequest(
            FibraId: _funoId,
            Period: "Q2-2025",
            CapRate: null,
            NavPerCbfi: null,
            Ltv: null,
            NoiMargin: null,
            FfoMargin: null,
            QuarterlyDistribution: null,
            Summary: "Solo resumen manual",
            PdfReference: null);

        var response = await _adminClient.PostAsJsonAsync("/api/v1/ops/fundamentals/import", payload);
        var body = await response.Content.ReadFromJsonAsync<FundamentalPreviewDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("partial", body!.Status);
        Assert.Empty(body.PresentFields);
        Assert.Equal(6, body.MissingFields.Count);
    }

    private async Task<string> LoginAndGetTokenAsync(string email, string password)
        => await LoginAndGetTokenAsync(_factory, email, password);

    private static async Task<string> LoginAndGetTokenAsync(ApiWebFactory factory, string email, string password)
    {
        var loginResponse = await factory.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, password));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return loginBody!.AccessToken;
    }

    private static byte[] CreateBlankPdfBytes()
        => CreatePdfBytes(string.Empty);

    private static byte[] CreateTextPdfBytes(string text)
        => CreatePdfBytes($"BT\n/F1 12 Tf\n72 720 Td\n({EscapePdfText(text)}) Tj\nET");

    private static string EscapePdfText(string text)
        => text.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);

    private static byte[] CreatePdfBytes(string contentStream)
    {
        var objects = new List<string>
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n",
            "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>\nendobj\n",
            $"4 0 obj\n<< /Length {Encoding.ASCII.GetByteCount(contentStream)} >>\nstream\n{contentStream}\nendstream\nendobj\n",
            "5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n",
        };

        var builder = new StringBuilder();
        builder.Append("%PDF-1.4\n");

        var offsets = new List<int> { 0 };
        foreach (var obj in objects)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append(obj);
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append($"xref\n0 {objects.Count + 1}\n");
        builder.Append("0000000000 65535 f \n");
        for (var i = 1; i <= objects.Count; i++)
        {
            builder.Append($"{offsets[i]:D10} 00000 n \n");
        }

        builder.Append("trailer\n");
        builder.Append($"<< /Size {objects.Count + 1} /Root 1 0 R >>\n");
        builder.Append("startxref\n");
        builder.Append($"{xrefOffset}\n");
        builder.Append("%%EOF");

        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    private sealed class FundamentalsExtractApiWebFactory(StubKpiExtractorService stub) : ApiWebFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IKpiExtractorService>();
                services.AddSingleton<IKpiExtractorService>(stub);
            });
        }
    }

    private sealed class StubKpiExtractorService(KpiExtractionResult result) : IKpiExtractorService
    {
        public string? LastMarkdownContent { get; private set; }

        public Task<KpiExtractionResult> ExtractAsync(string markdownContent, CancellationToken ct, Guid? relatedEntityId = null)
        {
            LastMarkdownContent = markdownContent;
            return Task.FromResult(result);
        }
    }
}
