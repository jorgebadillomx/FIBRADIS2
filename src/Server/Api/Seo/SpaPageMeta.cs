namespace Api.Seo;

public record SpaPageMeta(
    string Title,
    string Description,
    string CanonicalPath,  // e.g. "/calculadora" — el middleware prefija con BaseUrl
    string? JsonLd = null,
    string? RobotsDirectives = null
);
