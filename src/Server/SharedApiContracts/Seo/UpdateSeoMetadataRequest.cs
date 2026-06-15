namespace SharedApiContracts.Seo;

// Edición manual de una fila SeoMetadata (AC-3/AC-6 de 12-1). Cada campo es nullable: null = "no
// cambiar". Un campo con valor se persiste Y marca su flag *_IsOverridden = true, de modo que la
// regeneración automática (auto-llenado/regen) ya no lo pise. RobotsDirectives se conserva primero
// por compatibilidad con el editor de robots de 12-11.
public record UpdateSeoMetadataRequest(
    string? RobotsDirectives,
    string? Title = null,
    string? MetaDescription = null,
    string? CanonicalPath = null,
    string? OgImageUrl = null,
    string? OgType = null,
    string? TwitterCard = null,
    string? JsonLd = null,
    bool? IsActive = null);
