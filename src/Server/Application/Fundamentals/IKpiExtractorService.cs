namespace Application.Fundamentals;

public interface IKpiExtractorService
{
    Task<KpiExtractionResult> ExtractAsync(string markdownContent, CancellationToken ct, Guid? relatedEntityId = null);
}
