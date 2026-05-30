using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Infrastructure.Integrations.Articles;

public static partial class NewsTextNormalizer
{
    private static readonly HashSet<string> BoilerplateLines = new(StringComparer.OrdinalIgnoreCase)
    {
        "compartir",
        "suscribete",
        "suscríbete",
        "leer tambien",
        "leer también",
        "ver mas",
        "ver más",
        "contenido relacionado",
        "newsletter",
    };

    [GeneratedRegex(@"\r\n?")]
    private static partial Regex CarriageReturnRegex();

    [GeneratedRegex(@"^[ \t]*[-=*_]{3,}[ \t]*$", RegexOptions.Multiline)]
    private static partial Regex DecorativeSeparatorRegex();

    [GeneratedRegex(@"[ \t]{2,}")]
    private static partial Regex MultiSpaceRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex MultiBlankLineRegex();

    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        text = WebUtility.HtmlDecode(text);
        text = CarriageReturnRegex().Replace(text, "\n");
        text = DecorativeSeparatorRegex().Replace(text, string.Empty);

        var paragraphs = text
            .Split('\n')
            .Select(NormalizeLine)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        if (paragraphs.Count == 0)
            return string.Empty;

        var builder = new StringBuilder(text.Length);
        string? previous = null;

        foreach (var paragraph in paragraphs)
        {
            if (ShouldDropLine(paragraph))
                continue;

            if (previous is not null && string.Equals(previous, paragraph, StringComparison.OrdinalIgnoreCase))
                continue;

            if (builder.Length > 0)
                builder.Append('\n').Append('\n');

            builder.Append(paragraph);
            previous = paragraph;
        }

        var normalized = builder.ToString();
        normalized = MultiBlankLineRegex().Replace(normalized, "\n\n");
        return normalized.Trim();
    }

    private static string NormalizeLine(string line)
    {
        line = MultiSpaceRegex().Replace(line, " ");
        return line.Trim();
    }

    private static bool ShouldDropLine(string line)
    {
        if (line.Length == 0)
            return true;

        return line.Length <= 40
            && BoilerplateLines.Contains(RemoveAccents(line).ToLowerInvariant());
    }

    private static string RemoveAccents(string value)
    {
        Span<char> mapFrom = ['á', 'é', 'í', 'ó', 'ú', 'Á', 'É', 'Í', 'Ó', 'Ú'];
        Span<char> mapTo =   ['a', 'e', 'i', 'o', 'u', 'A', 'E', 'I', 'O', 'U'];

        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            for (var j = 0; j < mapFrom.Length; j++)
            {
                if (chars[i] == mapFrom[j])
                {
                    chars[i] = mapTo[j];
                    break;
                }
            }
        }

        return new string(chars);
    }
}
