using Domain.Catalog;
using Domain.News;
using Domain.Seo;

namespace Application.Seo;

public interface ISeoDefaultsBuilder
{
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

    string BuildFaqPageJsonLd(IReadOnlyList<FaqItem> items);
}
