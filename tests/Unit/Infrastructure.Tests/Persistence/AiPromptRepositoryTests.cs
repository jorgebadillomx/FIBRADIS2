using Domain.News;
using Infrastructure.Persistence.Repositories.News;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Persistence;

public class AiPromptRepositoryTests
{
    [Fact]
    public async Task GetPromptAsync_ReturnsSeededPrompt_WhenRecordExists()
    {
        await using var db = CreateDbContext();
        db.AiPrompts.Add(new AiPrompt
        {
            Id = 99,
            ContentType = "news",
            PromptTemplate = "Template existente",
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = "seed",
        });
        await db.SaveChangesAsync();

        var repo = new AiPromptRepository(db);
        var result = await repo.GetPromptAsync("news");

        Assert.NotNull(result);
        Assert.Equal("Template existente", result.PromptTemplate);
    }

    [Fact]
    public async Task SetPromptAsync_InsertsAndUpdatesPrompt()
    {
        await using var db = CreateDbContext();
        var repo = new AiPromptRepository(db);

        await repo.SetPromptAsync("document", "Plantilla v1", "jorge");
        await repo.SetPromptAsync("document", "Plantilla v2", "jorge");

        var stored = await db.AiPrompts.SingleAsync(x => x.ContentType == "document");
        Assert.Equal("Plantilla v2", stored.PromptTemplate);
        Assert.Equal("jorge", stored.UpdatedBy);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }
}
