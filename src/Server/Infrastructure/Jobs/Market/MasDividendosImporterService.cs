using System.Globalization;
using System.Text.RegularExpressions;
using Application.Catalog;
using Application.Market;
using Domain.Catalog;
using Infrastructure.Integrations.MasDividendos;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Jobs.Market;

public sealed class MasDividendosImporterService(
    IMasDividendosClient client,
    IMarketRepository marketRepo,
    ILogger<MasDividendosImporterService> logger) : IMasDividendosImporterService
{
    public async Task<MasDividendosImportResult> ImportAsync(IReadOnlyList<Fibra> fibras, CancellationToken ct = default)
    {
        if (fibras.Count == 0)
            return new MasDividendosImportResult(0, 0, 0);

        var records = await client.GetAllAsync(ct);
        var byTicker = BuildLookup(fibras);

        var updated = 0;
        var skipped = 0;
        var unmatched = 0;

        foreach (var record in records)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                if (record.FechaPago is null)
                {
                    skipped++;
                    continue;
                }

                var ticker = NormalizeTicker(record.Ticker);
                if (!TryMatchFibra(byTicker, fibras, ticker, out var fibra))
                {
                    unmatched++;
                    continue;
                }

                var amount = ParseAmount(record.Monto);
                var (taxable, capital) = MasDividendosCommentParser.Parse(record.Comentario, amount);
                var avisoUrl = NormalizeAvisoUrl(record.LinkAviso);

                var changed = await marketRepo.UpdateDistributionBreakdownAsync(
                    fibra.Id,
                    record.FechaPago.Value,
                    record.FechaExDerecho,
                    taxable,
                    capital,
                    avisoUrl,
                    ct);

                if (changed)
                    updated++;
                else
                    skipped++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Unable to import masdividendos record {RecordId}", record.Id ?? record.Ticker ?? "unknown");
                skipped++;
            }
        }

        return new MasDividendosImportResult(updated, skipped, unmatched);
    }

    private static Dictionary<string, Fibra> BuildLookup(IReadOnlyList<Fibra> fibras)
    {
        var lookup = new Dictionary<string, Fibra>(StringComparer.OrdinalIgnoreCase);
        foreach (var fibra in fibras)
        {
            foreach (var key in BuildKeys(fibra))
            {
                if (!lookup.ContainsKey(key))
                    lookup[key] = fibra;
            }
        }

        return lookup;
    }

    private static IEnumerable<string> BuildKeys(Fibra fibra)
    {
        var ticker = NormalizeTicker(fibra.Ticker);
        yield return ticker;
        yield return StripTrailingDigits(ticker);
    }

    private static bool TryMatchFibra(
        IReadOnlyDictionary<string, Fibra> lookup,
        IReadOnlyList<Fibra> fibras,
        string normalizedTicker,
        out Fibra fibra)
    {
        if (lookup.TryGetValue(normalizedTicker, out fibra!))
            return true;

        var baseTicker = StripTrailingDigits(normalizedTicker);
        if (lookup.TryGetValue(baseTicker, out fibra!))
            return true;

        if (baseTicker.Length == 0)
        {
            fibra = null!;
            return false;
        }

        fibra = fibras.FirstOrDefault(item =>
        {
            var ft = NormalizeTicker(item.Ticker);
            if (ft.Length < 4 || baseTicker.Length < 4)
                return false;
            return ft.Contains(baseTicker, StringComparison.OrdinalIgnoreCase)
                || baseTicker.Contains(ft, StringComparison.OrdinalIgnoreCase)
                || ft.StartsWith(baseTicker, StringComparison.OrdinalIgnoreCase)
                || baseTicker.StartsWith(ft, StringComparison.OrdinalIgnoreCase);
        })!;

        return fibra is not null;
    }

    private static string NormalizeTicker(string? ticker)
        => string.IsNullOrWhiteSpace(ticker)
            ? string.Empty
            : Regex.Replace(ticker.Trim().ToUpperInvariant(), @"[\d\*\s]+$", string.Empty);

    private static string StripTrailingDigits(string value)
        => Regex.Replace(value.Trim(), @"\d+$", string.Empty);

    private static decimal? ParseAmount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var cleaned = value
            .Replace("US$", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("$", string.Empty)
            .Replace(" ", string.Empty)
            .Trim();

        if (cleaned.Length == 0)
            return null;

        var commaCount = cleaned.Count(c => c == ',');
        if (commaCount == 1 && !cleaned.Contains('.'))
            cleaned = cleaned.Replace(',', '.');
        else
            cleaned = cleaned.Replace(",", string.Empty);

        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
            ? amount
            : null;
    }

    private static string? NormalizeAvisoUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
            return null;

        if (!uri.Host.EndsWith("bmv.com.mx", StringComparison.OrdinalIgnoreCase))
            return null;

        return uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            ? uri.ToString()
            : null;
    }
}
