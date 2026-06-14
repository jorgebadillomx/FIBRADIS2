using Domain.Seo;

namespace Application.Seo;

public sealed record SeoMetadataQuery(SeoPageType? PageType = null, string? Search = null);
