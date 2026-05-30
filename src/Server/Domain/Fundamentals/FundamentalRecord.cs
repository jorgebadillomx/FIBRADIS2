using System.Text.Json;

namespace Domain.Fundamentals;

public class FundamentalRecord
{
    public Guid Id { get; init; }
    public Guid FibraId { get; init; }
    public string Period { get; init; } = "";
    public string Status { get; set; } = "";
    public string ProcessingMode { get; init; } = "manual";
    public decimal? CapRate { get; set; }
    public decimal? NavPerCbfi { get; set; }
    public decimal? Ltv { get; set; }
    public decimal? NoiMargin { get; set; }
    public decimal? FfoMargin { get; set; }
    public decimal? QuarterlyDistribution { get; set; }
    public string? Summary { get; set; }
    public string? MarkdownContent { get; set; }
    public string? FieldNotesJson { get; private set; }
    public string? AiAnalysisJson { get; private set; }
    public string? PdfReference { get; set; }
    public DateTimeOffset? PdfUploadedAt { get; set; }
    public bool IsPossibleUpdate { get; init; }
    public string? ImportedBy { get; init; }
    public string? ConfirmedBy { get; set; }
    public DateTimeOffset CapturedAt { get; init; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public string? ErrorReason { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public void SetFieldNotes(Dictionary<string, string?>? notes)
    {
        FieldNotesJson = notes is { Count: > 0 }
            ? JsonSerializer.Serialize(notes)
            : null;
    }

    public Dictionary<string, string>? GetFieldNotes()
    {
        if (string.IsNullOrWhiteSpace(FieldNotesJson)) return null;
        try
        {
            var raw = JsonSerializer.Deserialize<Dictionary<string, string?>>(FieldNotesJson);
            if (raw is null) return null;
            var filtered = raw
                .Where(kv => kv.Value is not null)
                .ToDictionary(kv => kv.Key, kv => kv.Value!);
            return filtered.Count > 0 ? filtered : null;
        }
        catch (JsonException) { return null; }
    }

    public void SetAiAnalysis(FundamentalAiAnalysis? analysis)
    {
        AiAnalysisJson = analysis is not null
            ? JsonSerializer.Serialize(analysis)
            : null;
    }

    public FundamentalAiAnalysis? GetAiAnalysis()
    {
        if (string.IsNullOrWhiteSpace(AiAnalysisJson)) return null;
        try
        {
            return JsonSerializer.Deserialize<FundamentalAiAnalysis>(AiAnalysisJson);
        }
        catch (JsonException) { return null; }
    }
}
