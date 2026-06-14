namespace Domain.Seo;

public sealed class SeoMetadata
{
    public Guid Id { get; set; }
    public SeoPageType PageType { get; set; }
    public string EntityKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string MetaDescription { get; set; } = string.Empty;
    public string CanonicalPath { get; set; } = string.Empty;
    public string OgTitle { get; set; } = string.Empty;
    public string OgDescription { get; set; } = string.Empty;
    public string OgType { get; set; } = string.Empty;
    public string OgImageUrl { get; set; } = string.Empty;
    public string OgLocale { get; set; } = string.Empty;
    public string TwitterCard { get; set; } = string.Empty;
    public string RobotsDirectives { get; set; } = string.Empty;
    public string? JsonLd { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public bool TitleIsOverridden { get; set; }
    public bool MetaDescriptionIsOverridden { get; set; }
    public bool CanonicalPathIsOverridden { get; set; }
    public bool OgTitleIsOverridden { get; set; }
    public bool OgDescriptionIsOverridden { get; set; }
    public bool OgTypeIsOverridden { get; set; }
    public bool OgImageUrlIsOverridden { get; set; }
    public bool OgLocaleIsOverridden { get; set; }
    public bool TwitterCardIsOverridden { get; set; }
    public bool RobotsDirectivesIsOverridden { get; set; }
    public bool JsonLdIsOverridden { get; set; }
}
