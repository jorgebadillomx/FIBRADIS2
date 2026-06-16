using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Domain.Seo;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharedApiContracts.Auth;
using SharedApiContracts.Seo;

namespace Api.Tests;

// Cobertura de integración de AC-7 (backfill idempotente) y AC-3/AC-6 (PUT full-field + override)
// del módulo SEO administrable (12-1).
public class SeoBackfillEndpointTests
{
    [Fact]
    public async Task Backfill_RequiresAdminOps()
    {
        using var factory = new ApiWebFactory();
        await factory.SeedUsersAsync();

        var anonClient = factory.CreateClient();
        var userClient = await CreateAuthenticatedClientAsync(factory, "user@test.com", "password123");

        var anonResponse = await anonClient.PostAsync("/api/v1/ops/seo/backfill", null);
        var userResponse = await userClient.PostAsync("/api/v1/ops/seo/backfill", null);

        Assert.Equal(HttpStatusCode.Unauthorized, anonResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, userResponse.StatusCode);
    }

    [Fact]
    public async Task Backfill_CreatesRowsForFibras_AndIsIdempotent()
    {
        using var factory = new ApiWebFactory();
        await factory.SeedUsersAsync();
        await factory.SeedCatalogAsync();
        await factory.SeedNewsAsync();

        var adminClient = await CreateAuthenticatedClientAsync(factory, "adminops@test.com", "ops123");

        var firstResponse = await adminClient.PostAsync("/api/v1/ops/seo/backfill", null);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        var first = await firstResponse.Content.ReadFromJsonAsync<SeoBackfillResultDto>();
        Assert.NotNull(first);
        // HasData siembra fibras activas de producción ⇒ el backfill debe crear sus filas.
        Assert.True(first!.Fibras > 0, "El backfill debe crear filas SEO para las fibras activas seeded.");
        Assert.True(first.News >= 1, "El backfill debe crear la fila SEO de la noticia seeded.");

        // Segunda ejecución: idempotente, no crea nada nuevo.
        var secondResponse = await adminClient.PostAsync("/api/v1/ops/seo/backfill", null);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        var second = await secondResponse.Content.ReadFromJsonAsync<SeoBackfillResultDto>();
        Assert.NotNull(second);
        Assert.Equal(0, second!.Fibras);
        Assert.Equal(0, second.News);
        Assert.Equal(0, second.StaticPages);
    }

    [Fact]
    public async Task PutOpsSeo_FullField_PersistsAndMarksOverrides()
    {
        using var factory = new ApiWebFactory();
        await factory.SeedUsersAsync();

        var adminClient = await CreateAuthenticatedClientAsync(factory, "adminops@test.com", "ops123");
        var row = await SeedStaticRowAsync(factory);

        var request = new UpdateSeoMetadataRequest(
            RobotsDirectives: null,
            Title: "Título SEO editado a mano",
            MetaDescription: "Descripción editada manualmente desde Ops con la longitud adecuada para superar el piso de ciento veinte caracteres del proyecto SEO.",
            JsonLd: """{"@context":"https://schema.org","@type":"WebPage"}""");

        var response = await adminClient.PutAsJsonAsync($"/api/v1/ops/seo/{row.Id}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<SeoMetadataDto>();
        Assert.NotNull(updated);
        Assert.Equal("Título SEO editado a mano", updated!.Title);
        Assert.True(updated.TitleIsOverridden);
        Assert.True(updated.MetaDescriptionIsOverridden);
        Assert.True(updated.JsonLdIsOverridden);
        // og:title == title (regla de convenciones) se mantiene tras la edición.
        Assert.Equal(updated.Title, updated.OgTitle);
    }

    [Fact]
    public async Task PutOpsSeo_DescriptionTooLong_ReturnsBadRequest()
    {
        using var factory = new ApiWebFactory();
        await factory.SeedUsersAsync();

        var adminClient = await CreateAuthenticatedClientAsync(factory, "adminops@test.com", "ops123");
        var row = await SeedStaticRowAsync(factory);

        var request = new UpdateSeoMetadataRequest(
            RobotsDirectives: null,
            MetaDescription: new string('x', 161));

        var response = await adminClient.PutAsJsonAsync($"/api/v1/ops/seo/{row.Id}", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutOpsSeo_InvalidJsonLd_ReturnsBadRequest()
    {
        using var factory = new ApiWebFactory();
        await factory.SeedUsersAsync();

        var adminClient = await CreateAuthenticatedClientAsync(factory, "adminops@test.com", "ops123");
        var row = await SeedStaticRowAsync(factory);

        var request = new UpdateSeoMetadataRequest(
            RobotsDirectives: null,
            JsonLd: "{ esto no es json válido ");

        var response = await adminClient.PutAsJsonAsync($"/api/v1/ops/seo/{row.Id}", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutOpsSeo_EmptyTitle_ReturnsBadRequest()
    {
        using var factory = new ApiWebFactory();
        await factory.SeedUsersAsync();

        var adminClient = await CreateAuthenticatedClientAsync(factory, "adminops@test.com", "ops123");
        var row = await SeedStaticRowAsync(factory);

        // Title provisto pero vacío tras trim: marcaría override con "" → <title> vacío permanente.
        var request = new UpdateSeoMetadataRequest(RobotsDirectives: null, Title: "   ");

        var response = await adminClient.PutAsJsonAsync($"/api/v1/ops/seo/{row.Id}", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutOpsSeo_OgTypeTooLong_ReturnsBadRequest()
    {
        using var factory = new ApiWebFactory();
        await factory.SeedUsersAsync();

        var adminClient = await CreateAuthenticatedClientAsync(factory, "adminops@test.com", "ops123");
        var row = await SeedStaticRowAsync(factory);

        // OgType excede nvarchar(32): sin guard de validación sería DbUpdateException/500.
        var request = new UpdateSeoMetadataRequest(RobotsDirectives: null, OgType: new string('x', 33));

        var response = await adminClient.PutAsJsonAsync($"/api/v1/ops/seo/{row.Id}", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<HttpClient> CreateAuthenticatedClientAsync(ApiWebFactory factory, string email, string password)
    {
        var loginClient = factory.CreateClient();
        var loginResponse = await loginClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.AccessToken);
        return client;
    }

    private static async Task<SeoMetadata> SeedStaticRowAsync(ApiWebFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        var row = new SeoMetadata
        {
            Id = Guid.NewGuid(),
            PageType = SeoPageType.StaticPage,
            EntityKey = "/acerca",
            Title = "Sobre Fibras Inmobiliarias — Metodología y Fuentes de Datos | Fibras Inmobiliarias",
            MetaDescription = "Conoce la metodología de Fibras Inmobiliarias: fuentes de datos y cálculo de fundamentales para FIBRAs mexicanas.",
            CanonicalPath = "/acerca",
            OgTitle = "Sobre Fibras Inmobiliarias — Metodología y Fuentes de Datos | Fibras Inmobiliarias",
            OgDescription = "Conoce la metodología de Fibras Inmobiliarias.",
            OgType = "website",
            OgImageUrl = "https://fibradis.mx/og-image.png",
            OgLocale = "es_MX",
            TwitterCard = "summary_large_image",
            RobotsDirectives = "index,follow,max-image-preview:large,max-snippet:-1,max-video-preview:-1",
            JsonLd = null,
            IsActive = true,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = "test",
        };
        db.SeoMetadata.Add(row);
        await db.SaveChangesAsync();
        return row;
    }
}
