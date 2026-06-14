using Domain.Catalog;
using Domain.Fundamentals;
using Domain.News;
using Domain.Seo;

namespace Application.Seo;

public interface ISeoDefaultsBuilder
{
    string BuildBreadcrumbListJsonLd(string baseUrl, IReadOnlyList<SeoBreadcrumbItem> items);

    SeoMetadata BuildStaticPage(
        SeoPageType pageType,
        string entityKey,
        string title,
        string description,
        string canonicalPath,
        string? jsonLd,
        string baseUrl,
        DateTimeOffset updatedAt,
        string updatedBy = "system");

    SeoMetadata BuildFibra(
        Fibra fibra,
        string baseUrl,
        DateTimeOffset updatedAt,
        string updatedBy = "system",
        FibraSeoMarketData? marketData = null);

    SeoMetadata BuildNews(
        NewsArticle article,
        string baseUrl,
        DateTimeOffset updatedAt,
        string updatedBy = "system");

    string BuildComparePageJsonLd(
        IReadOnlyList<(string FullName, string Ticker)> fibras,
        string baseUrl);

    string BuildFundamentalesPageJsonLd(
        IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)> rows,
        string baseUrl);

    string BuildFaqPageJsonLd(IReadOnlyList<FaqItem> items);
}
