using Application.News;
using SharedApiContracts.News;

namespace Api.Endpoints.Public;

public static class NewsEndpoints
{
    public static IEndpointRouteBuilder MapNews(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/news").WithTags("News");

        group.MapGet("/", async (
            INewsRepository newsRepo,
            CancellationToken ct) =>
        {
            var articles = await newsRepo.GetLatestAsync(10, ct);
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

        return app;
    }

    private static NewsArticleDto ToDto(Domain.News.NewsArticle article)
        => new(article.Id, article.Title, article.Source, article.PublishedAt, article.Url, article.Snippet);
}
