using System.Security.Cryptography;
using System.Text.Json;
using Application.Catalog;
using Application.Fundamentals;
using Application.Jobs;
using Domain.Catalog;
using Domain.Fundamentals;
using Domain.Jobs;
using Infrastructure.Integrations.Pdf;
using Infrastructure.Integrations.PdfDiscovery;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Jobs.Fundamentals;

public class FundamentalsAutomationService(
    IAmefibraDiscoveryClient discoveryClient,
    IFibraRepository fibraRepo,
    IFundamentalRepository fundamentalRepo,
    IFundamentalSourceManifestRepository manifestRepo,
    IKpiExtractorService kpiExtractor,
    IPipelineErrorLogRepository errorLogRepo,
    IConfiguration config,
    ILogger<FundamentalsAutomationService> logger) : IFundamentalsAutomationService
{
    public async Task<FundamentalsAutomationRunResult> ExecuteAsync(CancellationToken ct)
    {
        var fibras = await fibraRepo.GetAllActiveAsync(ct);
        var listings = await discoveryClient.GetListingItemsAsync(ct);

        var scannedFibraIds = new HashSet<Guid>();
        var newReports = 0;
        var skippedReports = 0;
        var possibleUpdates = 0;
        var annualReports = 0;
        var ambiguousReports = 0;
        var errors = 0;
        var processed = 0;

        foreach (var listing in listings)
        {
            var parse = AmefibraTitleParser.Parse(listing.Title);
            var matchedFibra = MatchFibra(fibras, parse.FibraHint);

            var existingManifest = await manifestRepo.GetByPackageUrlAsync(listing.PackageUrl, ct);
            var manifest = existingManifest ?? new FundamentalSourceManifest
            {
                Id = Guid.NewGuid(),
                SourceName = "AMEFIBRA",
                PackageUrl = listing.PackageUrl,
                SourceTitle = listing.Title,
                FirstSeenAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            manifest.SourceTitle = listing.Title;
            manifest.FibraId = matchedFibra?.Id;
            manifest.Period = parse.Period;
            manifest.ReportType = parse.ReportType;
            manifest.DiscoveryStatus = matchedFibra is null && parse.DiscoveryStatus == "eligible"
                ? "unmatched-fibra"
                : parse.DiscoveryStatus;
            manifest.DownloadUrl = listing.DownloadUrl;
            manifest.DownloadSignature = AmefibraTitleParser.NormalizeDownloadSignature(listing.DownloadUrl);
            manifest.LastSeenAt = DateTimeOffset.UtcNow;
            manifest.UpdatedAt = DateTimeOffset.UtcNow;

            if (matchedFibra is not null)
                scannedFibraIds.Add(matchedFibra.Id);

            if (existingManifest is not null)
            {
                manifest.LastDecision = "skip";
                manifest.LastDecisionReason = "La entrada ya existe por packageUrl.";
                skippedReports++;
                await manifestRepo.UpdateAsync(manifest, ct);
                continue;
            }

            if (parse.ReportType == "annual")
            {
                annualReports++;
                manifest.LastDecision = "annual";
                manifest.LastDecisionReason = "El reporte fue clasificado como anual y no entra al flujo trimestral.";
                await HydrateDetailsAsync(manifest, ct);
                await manifestRepo.AddAsync(manifest, ct);
                continue;
            }

            if (parse.DiscoveryStatus != "eligible")
            {
                ambiguousReports++;
                manifest.LastDecision = "pending-classification";
                manifest.LastDecisionReason = parse.ErrorReason;
                manifest.LastError = parse.ErrorReason;
                await HydrateDetailsAsync(manifest, ct);
                await manifestRepo.AddAsync(manifest, ct);
                continue;
            }

            if (matchedFibra is null || parse.Period is null)
            {
                ambiguousReports++;
                manifest.LastDecision = "pending-classification";
                manifest.LastDecisionReason = matchedFibra is null
                    ? "No se pudo mapear el título a una FIBRA activa del catálogo."
                    : "No se pudo normalizar el período trimestral.";
                manifest.LastError = manifest.LastDecisionReason;
                await HydrateDetailsAsync(manifest, ct);
                await manifestRepo.AddAsync(manifest, ct);
                continue;
            }

            await HydrateDetailsAsync(manifest, ct);

            var samePeriodManifest = await manifestRepo.GetLatestByFibraAndPeriodAsync(matchedFibra.Id, parse.Period, ct);
            var existingProcessedRecord = await fundamentalRepo.GetProcessedByFibraAndPeriodAsync(matchedFibra.Id, parse.Period, ct);
            var isPossibleUpdate = samePeriodManifest is not null || existingProcessedRecord is not null;

            try
            {
                var recordId = await IngestAsync(matchedFibra, parse.Period, manifest, isPossibleUpdate, ct);
                manifest.LastProcessedRecordId = recordId;
                manifest.LastDecision = isPossibleUpdate ? "possibleUpdate" : "new";
                manifest.LastDecisionReason = isPossibleUpdate
                    ? "Mismo período con packageUrl distinto; marcado como possibleUpdate."
                    : "Nuevo período detectado y procesado.";
                manifest.LastError = null;

                if (isPossibleUpdate)
                    possibleUpdates++;
                else
                    newReports++;

                processed++;
                await manifestRepo.AddAsync(manifest, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                errors++;
                manifest.LastDecision = "error";
                manifest.LastDecisionReason = ex.Message;
                manifest.LastError = ex.Message;
                await TryLogPipelineErrorAsync(listing, matchedFibra, parse.Period, ex, ct);
                await manifestRepo.AddAsync(manifest, CancellationToken.None);
            }
        }

        return new FundamentalsAutomationRunResult(
            scannedFibraIds.Count,
            listings.Count,
            newReports,
            skippedReports,
            possibleUpdates,
            annualReports,
            ambiguousReports,
            errors,
            processed);
    }

    private async Task<Guid> IngestAsync(Fibra fibra, string period, FundamentalSourceManifest manifest, bool isPossibleUpdate, CancellationToken ct)
    {
        var downloadUrl = manifest.DownloadUrl;
        if (string.IsNullOrWhiteSpace(downloadUrl))
            throw new InvalidOperationException("El package de AMEFIBRA no expuso data-downloadurl.");

        var (pdfContent, pdfUrl, fileName) = await discoveryClient.DownloadPdfAsync(manifest.PackageUrl, downloadUrl, ct);
        manifest.PdfUrl = pdfUrl;
        manifest.FileName = fileName ?? BuildFallbackFileName(fibra.Ticker, period, pdfContent);

        var recordId = Guid.NewGuid();
        var relativePath = $"uploads/fundamentals/{recordId}.pdf";
        var markdown = ExtractMarkdown(pdfContent);

        var record = new FundamentalRecord
        {
            Id = recordId,
            FibraId = fibra.Id,
            Period = period,
            Status = string.IsNullOrWhiteSpace(markdown) ? "error" : "pending",
            ProcessingMode = "api",
            MarkdownContent = markdown,
            PdfReference = relativePath,
            PdfUploadedAt = DateTimeOffset.UtcNow,
            IsPossibleUpdate = isPossibleUpdate,
            ImportedBy = "system:amefibra",
            CapturedAt = DateTimeOffset.UtcNow,
            ErrorReason = string.IsNullOrWhiteSpace(markdown)
                ? "El PDF no produjo MarkdownContent; se requiere revisión manual."
                : null,
        };

        await fundamentalRepo.AddAsync(record, ct);
        await SavePdfAsync(recordId, pdfContent, ct);

        if (string.IsNullOrWhiteSpace(markdown))
            return recordId;

        var result = await kpiExtractor.ExtractAsync(markdown, ct, recordId);
        await fundamentalRepo.UpdateKpiExtractionAsync(recordId, result, ct);

        if (result.Success)
        {
            if (isPossibleUpdate)
            {
                var existingProcessed = await fundamentalRepo.GetProcessedByFibraAndPeriodAsync(fibra.Id, period, ct);
                if (existingProcessed is not null && existingProcessed.Id != recordId)
                    await fundamentalRepo.SoftDeleteAsync(existingProcessed.Id, "system:amefibra", ct);
            }

            await fundamentalRepo.UpdateStatusAsync(recordId, "processed", "system:amefibra", DateTimeOffset.UtcNow, ct);
        }

        return recordId;
    }

    private async Task HydrateDetailsAsync(FundamentalSourceManifest manifest, CancellationToken ct)
    {
        var details = await discoveryClient.GetPackageDetailsAsync(manifest.PackageUrl, ct);
        manifest.DownloadUrl = details.DownloadUrl ?? manifest.DownloadUrl;
        manifest.DownloadSignature = AmefibraTitleParser.NormalizeDownloadSignature(manifest.DownloadUrl);
        manifest.SourcePublishedAt = details.SourcePublishedAt;
    }

    private async Task SavePdfAsync(Guid recordId, byte[] content, CancellationToken ct)
    {
        var basePath = config["Uploads:BasePath"] ?? "wwwroot/uploads/fundamentals";
        Directory.CreateDirectory(basePath);
        var fullPath = Path.Combine(basePath, $"{recordId}.pdf");
        await File.WriteAllBytesAsync(fullPath, content, ct);
    }

    private static string? ExtractMarkdown(byte[] pdfContent)
    {
        using var stream = new MemoryStream(pdfContent);
        var markdown = MarkdownCompactor.Compact(PdfMarkdownExtractor.Extract(stream));
        return string.IsNullOrWhiteSpace(markdown) ? null : markdown;
    }

    private static string BuildFallbackFileName(string ticker, string period, byte[] content)
    {
        var hash = Convert.ToHexString(SHA256.HashData(content))[..12].ToLowerInvariant();
        return $"{ticker}-{period}-{hash}.pdf";
    }

    private Fibra? MatchFibra(IReadOnlyList<Fibra> fibras, string? fibraHint)
    {
        if (string.IsNullOrWhiteSpace(fibraHint))
            return null;

        var normalizedHint = NormalizeMatchKey(fibraHint);

        return fibras
            .Select(fibra =>
            {
                var candidates = new[]
                    {
                        fibra.Ticker,
                        fibra.Ticker.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9'),
                        fibra.ShortName,
                        fibra.FullName,
                    }
                    .Concat(fibra.NameVariants)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(NormalizeMatchKey)
                    .Distinct(StringComparer.Ordinal);

                var best = candidates.Aggregate(0.0, (acc, c) =>
                {
                    double s = 0;
                    if (c.Contains(normalizedHint, StringComparison.Ordinal))
                        s = Math.Max(s, (double)normalizedHint.Length / c.Length);
                    if (normalizedHint.Contains(c, StringComparison.Ordinal))
                        s = Math.Max(s, (double)c.Length / normalizedHint.Length);
                    return Math.Max(acc, s);
                });
                return (Fibra: fibra, Score: best);
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Fibra.Ticker, StringComparer.Ordinal)
            .Select(x => x.Fibra)
            .FirstOrDefault();
    }

    private static string NormalizeMatchKey(string value)
        => string.Concat(value
            .ToLowerInvariant()
            .Normalize(System.Text.NormalizationForm.FormD)
            .Where(ch => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(ch)));

    private async Task TryLogPipelineErrorAsync(AmefibraListingItem listing, Fibra? fibra, string? period, Exception ex, CancellationToken ct)
    {
        logger.LogWarning(ex, "Fundamentals pipeline failed for {Title}", listing.Title);
        try
        {
            await errorLogRepo.LogErrorAsync(new PipelineErrorLog
            {
                Pipeline = "Fundamentals",
                Timestamp = DateTimeOffset.UtcNow,
                ErrorType = ex.GetType().Name.Length > 100 ? ex.GetType().Name[..100] : ex.GetType().Name,
                Message = ex.Message,
                Context = JsonSerializer.Serialize(new
                {
                    listing.Title,
                    listing.PackageUrl,
                    fibraId = fibra?.Id,
                    ticker = fibra?.Ticker,
                    period,
                }),
                AiContext = fibra is not null
                    ? $"La corrida automática de fundamentales falló al procesar '{listing.Title}' para {fibra.Ticker} / {period}. Revise parseo, descarga del PDF o extracción IA."
                    : $"La corrida automática de fundamentales falló al procesar '{listing.Title}'. No se identificó la FIBRA.",
            }, ct);
        }
        catch (Exception logEx)
        {
            logger.LogWarning(logEx, "No se pudo persistir PipelineErrorLog de Fundamentals.");
        }
    }
}
