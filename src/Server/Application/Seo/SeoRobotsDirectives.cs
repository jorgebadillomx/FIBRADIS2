using System.Globalization;

namespace Application.Seo;

public static class SeoRobotsDirectives
{
    public const string IndexableRecommended = "index,follow,max-image-preview:large,max-snippet:-1,max-video-preview:-1";
    public const string NoIndex = "noindex,nofollow";
    public const string IndexWithoutSnippet = "index,follow,max-snippet:0";

    private const int MaxLength = 256;

    public static bool TryNormalize(string? value, out string normalized, out Dictionary<string, string[]> errors)
    {
        errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(value))
        {
            normalized = string.Empty;
            return true;
        }

        if (value.Trim().Length > MaxLength)
        {
            errors["robotsDirectives"] = [$"El campo robotsDirectives no puede superar {MaxLength} caracteres."];
            normalized = string.Empty;
            return false;
        }

        var tokens = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => token.Trim())
            .Where(token => token.Length > 0)
            .ToArray();

        if (tokens.Length == 0)
        {
            normalized = string.Empty;
            return true;
        }

        var hasIndex = false;
        var hasNoIndex = false;
        var hasFollow = false;
        var hasNoFollow = false;
        var hasNoArchive = false;
        var hasNoSnippet = false;
        var hasNoImageIndex = false;
        int? maxSnippet = null;
        int? maxVideoPreview = null;
        string? maxImagePreview = null;
        var errorsList = new List<string>();

        foreach (var rawToken in tokens)
        {
            var token = rawToken.Trim();
            var lower = token.ToLowerInvariant();

            switch (lower)
            {
                case "index":
                    hasIndex = true;
                    continue;
                case "noindex":
                    hasNoIndex = true;
                    continue;
                case "follow":
                    hasFollow = true;
                    continue;
                case "nofollow":
                    hasNoFollow = true;
                    continue;
                case "all":
                    hasIndex = true;
                    hasFollow = true;
                    continue;
                case "none":
                    hasNoIndex = true;
                    hasNoFollow = true;
                    continue;
                case "noarchive":
                    hasNoArchive = true;
                    continue;
                case "nosnippet":
                    hasNoSnippet = true;
                    continue;
                case "noimageindex":
                    hasNoImageIndex = true;
                    continue;
            }

            if (lower.StartsWith("max-snippet:", StringComparison.Ordinal))
            {
                if (!TryParseNumericDirective(token, out var parsedSnippet))
                {
                    errorsList.Add($"Valor inválido para {token}.");
                    continue;
                }

                maxSnippet = parsedSnippet;
                continue;
            }

            if (lower.StartsWith("max-image-preview:", StringComparison.Ordinal))
            {
                var valuePart = token[(token.IndexOf(':') + 1)..].Trim().ToLowerInvariant();
                if (valuePart is not ("none" or "standard" or "large"))
                {
                    errorsList.Add($"Valor inválido para {token}.");
                    continue;
                }

                maxImagePreview = valuePart;
                continue;
            }

            if (lower.StartsWith("max-video-preview:", StringComparison.Ordinal))
            {
                if (!TryParseNumericDirective(token, out var parsedVideoPreview))
                {
                    errorsList.Add($"Valor inválido para {token}.");
                    continue;
                }

                maxVideoPreview = parsedVideoPreview;
                continue;
            }

            errorsList.Add($"La directiva '{token}' no es válida.");
        }

        if (hasIndex && hasNoIndex)
            errorsList.Add("Las directivas index y noindex no pueden coexistir.");

        if (hasFollow && hasNoFollow)
            errorsList.Add("Las directivas follow y nofollow no pueden coexistir.");

        if (errorsList.Count > 0)
        {
            errors["robotsDirectives"] = errorsList.ToArray();
            normalized = string.Empty;
            return false;
        }

        var normalizedTokens = new List<string>(capacity: 8);
        normalizedTokens.Add(hasNoIndex ? "noindex" : "index");
        normalizedTokens.Add(hasNoFollow ? "nofollow" : "follow");

        if (maxImagePreview is not null)
            normalizedTokens.Add(FormattableString.Invariant($"max-image-preview:{maxImagePreview}"));

        if (maxSnippet is not null)
            normalizedTokens.Add(FormattableString.Invariant($"max-snippet:{maxSnippet.Value}"));

        if (maxVideoPreview is not null)
            normalizedTokens.Add(FormattableString.Invariant($"max-video-preview:{maxVideoPreview.Value}"));

        if (hasNoArchive)
            normalizedTokens.Add("noarchive");

        if (hasNoSnippet)
            normalizedTokens.Add("nosnippet");

        if (hasNoImageIndex)
            normalizedTokens.Add("noimageindex");

        normalized = string.Join(',', normalizedTokens);
        return true;
    }

    private static bool TryParseNumericDirective(string token, out int value)
    {
        value = default;
        var separatorIndex = token.IndexOf(':');
        if (separatorIndex < 0 || separatorIndex == token.Length - 1)
            return false;

        var rawValue = token[(separatorIndex + 1)..].Trim();
        if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            return false;

        return value >= -1;
    }
}
