using System.Text.Json;
using Application.News;
using Domain.News;
using SharedApiContracts.News;

namespace Api.Endpoints.Public;

public static class NewsEndpoints
{
    private static readonly JsonSerializerOptions AnalysisDeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static IEndpointRouteBuilder MapNews(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/news").WithTags("News");

        group.MapGet("/", async (
            INewsRepository newsRepo,
            CancellationToken ct) =>
        {
            var articles = await newsRepo.GetLatestAsync(5, ct);
            return Results.Ok(articles.Select(ToDto).ToList());
        })
        .AllowAnonymous()
        .Produces<IReadOnlyList<NewsArticleDto>>(StatusCodes.Status200OK);

        group.MapGet("/fibras/{fibraId:guid}", async (
            Guid fibraId,
            INewsRepository newsRepo,
            CancellationToken ct) =>
        {
            var articles = await newsRepo.GetLatestForFibraAsync(fibraId, 10, ct);
            return Results.Ok(articles.Select(ToDto).ToList());
        })
        .AllowAnonymous()
        .Produces<IReadOnlyList<NewsArticleDto>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", async (
            Guid id,
            INewsRepository newsRepo,
            CancellationToken ct) =>
        {
            var article = await newsRepo.GetByIdAsync(id, ct);
            if (article is null || article.DeletedAt is not null) return Results.NotFound();
            return Results.Ok(ToDto(article));
        })
        .AllowAnonymous()
        .Produces<NewsArticleDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static NewsArticleDto ToDto(NewsArticle article)
        => new(article.Id, article.Title, article.Source, article.PublishedAt, article.Url,
            article.Snippet, article.ImageUrl, article.AiSummary, MapAnalysis(article.AiAnalysisJson));

    private static NewsAiAnalysisDto? MapAnalysis(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<NewsAiAnalysisDto>(json, AnalysisDeserializeOptions);
        }
        catch
        {
            return null;
        }
    }
}
