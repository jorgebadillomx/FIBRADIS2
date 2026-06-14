namespace Application.Seo;

public static class UrlRedirectPath
{
    private static readonly string[] ReservedExactPaths =
    [
        "/",
        "/api",
        "/ops",
        "/fibras",
        "/hangfire",
        "/assets",
    ];

    private static readonly string[] ReservedPrefixes =
    [
        "/api/",
        "/ops/",
        "/fibras/",
        "/hangfire/",
        "/assets/",
    ];

    public static string Normalize(string path)
    {
        var normalized = path.Trim().ToLowerInvariant();
        if (normalized.Length == 0)
            return normalized;

        return normalized == "/" ? "/" : normalized.TrimEnd('/');
    }

    public static bool IsReservedSource(string normalizedPath) =>
        ReservedExactPaths.Any(path => normalizedPath.Equals(path, StringComparison.OrdinalIgnoreCase)) ||
        ReservedPrefixes.Any(prefix => normalizedPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    public static bool IsInternalPath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        if (!trimmed.StartsWith('/'))
            return false;

        // Backslashes y caracteres de control habilitan host-injection / open-redirect:
        // los navegadores normalizan "/\evil.com" o "/%5Cevil.com" a un "//" protocol-relative.
        if (trimmed.Any(c => c == '\\' || char.IsControl(c)))
            return false;

        // Rechazar protocol-relative ("//host") y su variante con backslash ("/\host").
        if (trimmed.Length >= 2 && (trimmed[1] == '/' || trimmed[1] == '\\'))
            return false;

        return !Uri.TryCreate(trimmed, UriKind.Absolute, out _);
    }
}
