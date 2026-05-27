using System.Globalization;
using System.Text.Json;
using Application.Fundamentals;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Integrations.Ai;

internal static class KpiExtractionJsonParser
{
    public static KpiExtractionResult Parse(string raw, ILogger logger, string providerName)
    {
        if (TryParse(raw, out var result))
        {
            return FillMissingNotes(result) with { Success = HasAnyExtractedValue(result) };
        }

        var jsonStart = raw.IndexOf('{');
        var jsonEnd = raw.LastIndexOf('}');
        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            var candidate = raw[jsonStart..(jsonEnd + 1)];
            if (TryParse(candidate, out result))
            {
                return FillMissingNotes(result) with { Success = HasAnyExtractedValue(result) };
            }
        }

        var truncated = raw.Length > 500 ? raw[..500] + "…" : raw;
        logger.LogError(
            "No se pudo parsear la respuesta JSON de {Provider}. Raw (primeros 500 chars): {RawResponse}",
            providerName,
            truncated);

        return new KpiExtractionResult(
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "La IA devolvió una respuesta inválida y no se pudo interpretar.",
            false);
    }

    public static int CountExtractedNumericFields(KpiExtractionResult result)
    {
        var count = 0;
        if (result.CapRate is not null) count++;
        if (result.NavPerCbfi is not null) count++;
        if (result.Ltv is not null) count++;
        if (result.NoiMargin is not null) count++;
        if (result.FfoMargin is not null) count++;
        if (result.QuarterlyDistribution is not null) count++;
        return count;
    }

    private static bool TryParse(string json, out KpiExtractionResult result)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            result = new KpiExtractionResult(
                ReadNullableDecimal(root, "capRate"),
                ReadNullableString(root, "capRateNote"),
                ReadNullableDecimal(root, "navPerCbfi"),
                ReadNullableString(root, "navPerCbfiNote"),
                ReadNullableDecimal(root, "ltv"),
                ReadNullableString(root, "ltvNote"),
                ReadNullableDecimal(root, "noiMargin"),
                ReadNullableString(root, "noiMarginNote"),
                ReadNullableDecimal(root, "ffoMargin"),
                ReadNullableString(root, "ffoMarginNote"),
                ReadNullableDecimal(root, "quarterlyDistribution"),
                ReadNullableString(root, "quarterlyDistributionNote"),
                ReadNullableString(root, "summary"),
                ReadNullableString(root, "extractionNotes") ?? "La IA no devolvió notas de extracción.",
                false);

            return true;
        }
        catch (JsonException)
        {
            result = new KpiExtractionResult(null, null, null, null, null, null, null, null, null, null, null, null, null, string.Empty, false);
            return false;
        }
    }

    private static KpiExtractionResult FillMissingNotes(KpiExtractionResult result)
    {
        const string fallback = "Nota no disponible para este campo.";
        return result with
        {
            CapRateNote       = result.CapRate is not null && result.CapRateNote is null ? fallback : result.CapRateNote,
            NavPerCbfiNote    = result.NavPerCbfi is not null && result.NavPerCbfiNote is null ? fallback : result.NavPerCbfiNote,
            LtvNote           = result.Ltv is not null && result.LtvNote is null ? fallback : result.LtvNote,
            NoiMarginNote     = result.NoiMargin is not null && result.NoiMarginNote is null ? fallback : result.NoiMarginNote,
            FfoMarginNote     = result.FfoMargin is not null && result.FfoMarginNote is null ? fallback : result.FfoMarginNote,
            QuarterlyDistributionNote = result.QuarterlyDistribution is not null && result.QuarterlyDistributionNote is null ? fallback : result.QuarterlyDistributionNote,
        };
    }

    private static bool HasAnyExtractedValue(KpiExtractionResult result)
        => CountExtractedNumericFields(result) > 0 || !string.IsNullOrWhiteSpace(result.Summary);

    private static decimal? ReadNullableDecimal(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
            return null;

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetDecimal(out var decimalValue) => decimalValue,
            JsonValueKind.String when decimal.TryParse(property.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedValue) => parsedValue,
            _ => null,
        };
    }

    private static string? ReadNullableString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
            return null;

        if (property.ValueKind == JsonValueKind.Null)
            return null;

        var value = property.GetString()?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
