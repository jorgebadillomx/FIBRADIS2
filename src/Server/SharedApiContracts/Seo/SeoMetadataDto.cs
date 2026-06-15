namespace SharedApiContracts.Seo;

public record SeoMetadataDto(
    Guid Id,
    string PageType,
    string EntityKey,
    string Title,
    string MetaDescription,
    string CanonicalPath,
    string RobotsDirectives,
    bool RobotsDirectivesIsOverridden,
    bool IsActive,
    DateTimeOffset UpdatedAt,
    string UpdatedBy,
    // Campos añadidos en 12-1 (cierre AC-3/AC-6): set completo editable + flags de override por campo.
    string OgTitle,
    string OgDescription,
    string OgType,
    string OgImageUrl,
    string OgLocale,
    string TwitterCard,
    string? JsonLd,
    bool TitleIsOverridden,
    bool MetaDescriptionIsOverridden,
    bool CanonicalPathIsOverridden,
    bool OgImageUrlIsOverridden,
    bool OgTypeIsOverridden,
    bool TwitterCardIsOverridden,
    bool JsonLdIsOverridden);
