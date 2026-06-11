using System.Text.Json;
using Application.News;
using Application.Ops;
using Domain.News;
using Microsoft.AspNetCore.Mvc;
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
            IOperationalConfigRepository configRepo,
            CancellationToken ct) =>
        {
            var config = await configRepo.GetAsync(ct);
            var articles = await newsRepo.GetLatestForFibraAsync(fibraId, 5, config.FibraNewsMonths, ct);
            return Results.Ok(articles.Select(ToDto).ToList());
        })
        .AllowAnonymous()
        .Produces<IReadOnlyList<NewsArticleDto>>(StatusCodes.Status200OK);

        group.MapGet("/paged", async (
            [AsParameters] NewsPagedRequest req,
            INewsRepository newsRepo,
            CancellationToken ct) =>
        {
            var page = Math.Max(1, req.PageNumber ?? 1);
            var pageSize = Math.Clamp(req.PageSize ?? 20, 1, 50);
            var (items, total, tickersByArticleId) = await newsRepo.GetPagedPublicAsync(page, pageSize, req.Q, req.FibraId, ct);

            var dtos = items
                .Select(article => ToDtoWithTickerNames(article, tickersByArticleId))
                .ToList();

            return Results.Ok(new NewsPagedResultDto(dtos, total, page, pageSize));
        })
        .AllowAnonymous()
        .Produces<NewsPagedResultDto>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", async (
            Guid id,
            INewsRepository newsRepo,
            CancellationToken ct) =>
        {
            var article = await newsRepo.GetByIdAsync(id, ct);
            if (article is null || article.DeletedAt is not null) return Results.NotFound();
            var fibras = await newsRepo.GetLinkedFibrasAsync(id, ct);
            return Results.Ok(ToDtoWithFibras(article, fibras));
        })
        .AllowAnonymous()
        .Produces<NewsArticleDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/related", async (
            Guid id,
            INewsRepository newsRepo,
            CancellationToken ct) =>
        {
            var articles = await newsRepo.GetRelatedAsync(id, 5, ct);
            return Results.Ok(articles.Select(ToDto).ToList());
        })
        .AllowAnonymous()
        .Produces<IReadOnlyList<NewsArticleDto>>(StatusCodes.Status200OK);

        // Sin constraint: captura todo lo que no sea GUID ni literal (/paged, /fibras).
        // ASP.NET Core prioriza literal > constraint > sin constraint — no hay ambigüedad.
        group.MapGet("/{slug}", async (
            string slug,
            INewsRepository newsRepo,
            CancellationToken ct) =>
        {
            var article = await newsRepo.GetBySlugAsync(slug, ct);
            if (article is null) return Results.NotFound();
            var fibras = await newsRepo.GetLinkedFibrasAsync(article.Id, ct);
            return Results.Ok(ToDtoWithFibras(article, fibras));
        })
        .AllowAnonymous()
        .Produces<NewsArticleDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static NewsArticleDto ToDto(NewsArticle article)
        => new(article.Id, article.Title, article.Source, article.PublishedAt, article.Url,
            article.Snippet, article.ImageUrl, article.AiSummary, MapAnalysis(article.AiAnalysisJson),
            Slug: article.Slug);

    private static NewsArticleDto ToDtoWithTickerNames(
        NewsArticle article,
        IReadOnlyDictionary<Guid, IReadOnlyList<(Guid FibraId, string Ticker)>> tickersByArticleId)
    {
        var linkedFibras = tickersByArticleId.TryGetValue(article.Id, out var tickers)
            ? tickers.Select(t => new LinkedFibraDto(t.FibraId, t.Ticker)).ToList()
            : null;

        return new NewsArticleDto(
            article.Id,
            article.Title,
            article.Source,
            article.PublishedAt,
            article.Url,
            article.Snippet,
            article.ImageUrl,
            article.AiSummary,
            MapAnalysis(article.AiAnalysisJson),
            linkedFibras,
            article.Slug);
    }

    private static NewsArticleDto ToDtoWithFibras(NewsArticle article, IReadOnlyList<(Guid Id, string Ticker)> fibras)
    {
        var linked = fibras.Count > 0
            ? fibras.Select(f => new LinkedFibraDto(f.Id, f.Ticker)).ToList()
            : null;
        return new(article.Id, article.Title, article.Source, article.PublishedAt, article.Url,
            article.Snippet, article.ImageUrl, article.AiSummary, MapAnalysis(article.AiAnalysisJson), linked,
            article.Slug);
    }

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

public sealed record NewsPagedRequest(
    [FromQuery(Name = "pageNumber")] int? PageNumber,
    [FromQuery(Name = "pageSize")] int? PageSize,
    [FromQuery(Name = "q")] string? Q,
    [FromQuery(Name = "fibraId")] Guid? FibraId
);
