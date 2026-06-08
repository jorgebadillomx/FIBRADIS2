using System.Net.Http.Headers;
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
    IEnumerable<IFundamentalsDiscoverySource> discoverySources,
    IFibraRepository fibraRepo,
    IFundamentalRepository fundamentalRepo,
    IFundamentalSourceManifestRepository manifestRepo,
    IKpiExtractorService kpiExtractor,
    IPipelineErrorLogRepository errorLogRepo,
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<FundamentalsAutomationService> logger) : IFundamentalsAutomationService
{
    public async Task<FundamentalsAutomationRunResult> ExecuteAsync(CancellationToken ct)
    {
        var fibras = await fibraRepo.GetAllActiveAsync(ct);
        var sources = discoverySources.ToList();

        var scannedFibraIds = new HashSet<Guid>();
        var totalCandidates = 0;
        var newReports = 0;
        var skippedReports = 0;
        var possibleUpdates = 0;
        var annualReports = 0;
        var ambiguousReports = 0;
        var errors = 0;
        var processed = 0;

        foreach (var fibra in fibras)
        {
            var applicableSources = sources
                .Where(s => !s.SupportedTickers.Any() || s.SupportedTickers.Contains(fibra.Ticker));

            foreach (var source in applicableSources)
            {
                IReadOnlyList<FundamentalsDiscoveryCandidate> candidates;
                try
                {
                    candidates = await source.DiscoverCandidatesAsync(fibra, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    errors++;
                    logger.LogWarning(ex, "Discovery source {Source} falló para {Ticker}", source.SourceName, fibra.Ticker);
                    await TryLogPipelineErrorAsync(source.SourceName, fibra, null, ex, ct);
                    continue;
                }

                totalCandidates += candidates.Count;
                var candidateIndex = 0;
                foreach (var candidate in candidates)
                {
                    if (candidateIndex++ > 0)
                        await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(4, 9)), ct);
                    var existingManifest = await manifestRepo.GetBySourceAndPackageUrlAsync(candidate.SourceName, candidate.PackageUrl, ct);
                    var manifest = existingManifest ?? new FundamentalSourceManifest
                    {
                        Id = Guid.NewGuid(),
                        SourceName = candidate.SourceName,
                        PackageUrl = candidate.PackageUrl,
                        SourceTitle = candidate.SourceTitle,
                        FirstSeenAt = DateTimeOffset.UtcNow,
                        CreatedAt = DateTimeOffset.UtcNow,
                    };

                    manifest.SourceTitle = candidate.SourceTitle;
                    manifest.FibraId = fibra.Id;
                    manifest.Period = candidate.Period;
                    manifest.ReportType = candidate.ReportType;
                    manifest.DownloadUrl = candidate.DownloadUrl;
                    manifest.DownloadSignature = NormalizeSignature(candidate.DownloadUrl);
                    manifest.SourcePublishedAt = candidate.PublishedAt;
                    manifest.LastSeenAt = DateTimeOffset.UtcNow;
                    manifest.UpdatedAt = DateTimeOffset.UtcNow;

                    scannedFibraIds.Add(fibra.Id);

                    if (existingManifest is not null)
                    {
                        manifest.LastDecision = "skip";
                        manifest.LastDecisionReason = "La entrada ya existe por packageUrl.";
                        skippedReports++;
                        await manifestRepo.UpdateAsync(manifest, ct);
                        continue;
                    }

                    manifest.DiscoveryStatus = DetermineDiscoveryStatus(candidate);

                    if (candidate.ReportType == "annual")
                    {
                        annualReports++;
                        manifest.LastDecision = "annual";
                        manifest.LastDecisionReason = "El reporte fue clasificado como anual y no entra al flujo trimestral.";
                        await manifestRepo.AddAsync(manifest, ct);
                        continue;
                    }

                    if (candidate.Period is null || manifest.DiscoveryStatus != "eligible")
                    {
                        ambiguousReports++;
                        manifest.LastDecision = "pending-classification";
                        manifest.LastDecisionReason = candidate.Period is null
                            ? "No se pudo normalizar el período trimestral."
                            : "Candidato marcado como no elegible por la fuente.";
                        manifest.LastError = manifest.LastDecisionReason;
                        await manifestRepo.AddAsync(manifest, ct);
                        continue;
                    }

                    var samePeriodManifest = await manifestRepo.GetLatestByFibraAndPeriodAsync(fibra.Id, candidate.Period, ct);
                    var existingProcessedRecord = await fundamentalRepo.GetProcessedByFibraAndPeriodAsync(fibra.Id, candidate.Period, ct);
                    var isPossibleUpdate = samePeriodManifest is not null || existingProcessedRecord is not null;

                    try
                    {
                        var recordId = await IngestAsync(fibra, candidate, manifest, isPossibleUpdate, ct);
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
                        await TryLogPipelineErrorAsync(candidate.SourceName, fibra, candidate.Period, ex, ct);
                        await manifestRepo.AddAsync(manifest, CancellationToken.None);
                    }
                }
            }
        }

        return new FundamentalsAutomationRunResult(
            scannedFibraIds.Count,
            totalCandidates,
            newReports,
            skippedReports,
            possibleUpdates,
            annualReports,
            ambiguousReports,
            errors,
            processed);
    }

    private async Task<Guid> IngestAsync(Fibra fibra, FundamentalsDiscoveryCandidate candidate, FundamentalSourceManifest manifest, bool isPossibleUpdate, CancellationToken ct)
    {
        var downloadUrl = candidate.DownloadUrl;
        if (string.IsNullOrWhiteSpace(downloadUrl))
            throw new InvalidOperationException($"El candidato de {candidate.SourceName} no tiene DownloadUrl.");

        var (pdfContent, pdfUrl, fileName) = await DownloadPdfAsync(downloadUrl, ct);
        manifest.PdfUrl = pdfUrl;
        manifest.FileName = fileName ?? BuildFallbackFileName(fibra.Ticker, candidate.Period ?? "unknown", pdfContent);

        var recordId = Guid.NewGuid();
        var relativePath = $"uploads/fundamentals/{recordId}.pdf";
        var markdown = ExtractMarkdown(pdfContent);

        var record = new FundamentalRecord
        {
            Id = recordId,
            FibraId = fibra.Id,
            Period = candidate.Period!,
            Status = string.IsNullOrWhiteSpace(markdown) ? "error" : "pending",
            ProcessingMode = "api",
            MarkdownContent = markdown,
            PdfReference = relativePath,
            PdfUploadedAt = DateTimeOffset.UtcNow,
            IsPossibleUpdate = isPossibleUpdate,
            ImportedBy = $"system:{candidate.SourceName}",
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
                var existingProcessed = await fundamentalRepo.GetProcessedByFibraAndPeriodAsync(fibra.Id, candidate.Period!, ct);
                if (existingProcessed is not null && existingProcessed.Id != recordId)
                    await fundamentalRepo.SoftDeleteAsync(existingProcessed.Id, $"system:{candidate.SourceName}", ct);
            }

            await fundamentalRepo.UpdateStatusAsync(recordId, "processed", $"system:{candidate.SourceName}", DateTimeOffset.UtcNow, ct);
        }

        return recordId;
    }

    private async Task<(byte[] Content, string? PdfUrl, string? FileName)> DownloadPdfAsync(string downloadUrl, CancellationToken ct)
    {
        var http = httpClientFactory.CreateClient("FundamentalsDownloader");
        using var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
        request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36");
        request.Headers.TryAddWithoutValidation("Accept-Language", "es-MX,es;q=0.9,en;q=0.8");
        request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br");
        request.Headers.TryAddWithoutValidation("Referer", "https://amefibra.com/reportes-de-fibras/");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.8));
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var finalUrl = response.RequestMessage?.RequestUri?.ToString();
        var fileName = AmefibraTitleParser.GetFileNameFromUrl(finalUrl);
        var content = await response.Content.ReadAsByteArrayAsync(ct);
        return (content, finalUrl ?? downloadUrl, fileName);
    }

    private static string DetermineDiscoveryStatus(FundamentalsDiscoveryCandidate candidate)
    {
        if (candidate.ReportType == "annual")
            return "annual";
        if (candidate.Period is null)
            return "pending-classification";
        return "eligible";
    }

    private static string NormalizeSignature(string? downloadUrl)
    {
        if (string.IsNullOrWhiteSpace(downloadUrl) || !Uri.TryCreate(downloadUrl, UriKind.Absolute, out var uri))
            return string.Empty;

        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var wpdmdl = query["wpdmdl"];
        return wpdmdl is null
            ? uri.GetLeftPart(UriPartial.Path)
            : $"{uri.GetLeftPart(UriPartial.Path)}?wpdmdl={wpdmdl}";
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

    private async Task TryLogPipelineErrorAsync(string sourceName, Fibra? fibra, string? period, Exception ex, CancellationToken ct)
    {
        logger.LogWarning(ex, "Fundamentals pipeline failed for {Source} / {Ticker}", sourceName, fibra?.Ticker);
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
                    source = sourceName,
                    fibraId = fibra?.Id,
                    ticker = fibra?.Ticker,
                    period,
                }),
                AiContext = fibra is not null
                    ? $"La corrida automática de fundamentales falló al procesar {sourceName} para {fibra.Ticker} / {period}. Revise descarga del PDF o extracción IA."
                    : $"La corrida automática de fundamentales falló al procesar {sourceName}. No se identificó la FIBRA.",
            }, ct);
        }
        catch (Exception logEx)
        {
            logger.LogWarning(logEx, "No se pudo persistir PipelineErrorLog de Fundamentals.");
        }
    }
}
