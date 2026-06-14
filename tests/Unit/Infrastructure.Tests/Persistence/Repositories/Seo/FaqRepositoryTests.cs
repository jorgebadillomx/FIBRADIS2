using Application.Seo;
using Domain.Seo;
using Infrastructure.Persistence.Repositories.Seo;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Persistence.Repositories.Seo;

public class FaqRepositoryTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    [Fact]
    public async Task GetByPageAsync_FiltersInactiveItems_AndOrdersByOrderThenQuestion()
    {
        await using var db = CreateDbContext();
        db.FaqItems.AddRange(
            CreateItem(order: 2, question: "¿Qué es Cap Rate?", answer: "Cap Rate"),
            CreateItem(order: 1, question: "¿Qué es NAV por CBFI?", answer: "NAV", isActive: true),
            CreateItem(order: 3, question: "¿Oculta?", answer: "No debe salir", isActive: false));
        await db.SaveChangesAsync();

        var repo = new FaqRepository(db);

        var items = await repo.GetByPageAsync(SeoPageType.StaticPage, "/fundamentales/", includeInactive: false);

        Assert.Equal(2, items.Count);
        Assert.Equal("¿Qué es NAV por CBFI?", items[0].Question);
        Assert.Equal("¿Qué es Cap Rate?", items[1].Question);
    }

    [Fact]
    public async Task AddIfMissingAsync_IsIdempotentForSameNaturalKey()
    {
        await using var db = CreateDbContext();
        var repo = new FaqRepository(db);

        var created = await repo.AddIfMissingAsync(CreateItem(order: 1, question: "¿Qué es LTV?", answer: "LTV"));
        var duplicate = await repo.AddIfMissingAsync(CreateItem(order: 2, question: "¿Qué es LTV?", answer: "LTV duplicado"));

        Assert.True(created);
        Assert.False(duplicate);
        Assert.Equal(1, await db.FaqItems.CountAsync());
    }

    [Fact]
    public async Task DeactivateAsync_DisablesItem_WithoutDeletingIt()
    {
        await using var db = CreateDbContext();
        var item = CreateItem(order: 1, question: "¿Qué es FFO Margin?", answer: "FFO");
        db.FaqItems.Add(item);
        await db.SaveChangesAsync();

        var repo = new FaqRepository(db);

        var result = await repo.DeactivateAsync(item.Id, "adminops@test.com");

        Assert.True(result);

        var persisted = await db.FaqItems.FirstAsync(x => x.Id == item.Id);
        Assert.False(persisted.IsActive);
        Assert.Equal("adminops@test.com", persisted.UpdatedBy);
        Assert.True(persisted.UpdatedAt > DateTimeOffset.MinValue);
    }

    private static FaqItem CreateItem(int order, string question, string answer, bool isActive = true) => new()
    {
        Id = Guid.NewGuid(),
        PageType = SeoPageType.StaticPage,
        // Sembrado sin slash final; GetByPageAsync recibe "/fundamentales/" y debe normalizar para hacer match.
        EntityKey = "/fundamentales",
        Question = question,
        Answer = answer,
        Order = order,
        IsActive = isActive,
        UpdatedAt = DateTimeOffset.UtcNow,
        UpdatedBy = "system",
    };
}
