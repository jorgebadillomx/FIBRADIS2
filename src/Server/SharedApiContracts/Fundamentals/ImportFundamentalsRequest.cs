namespace SharedApiContracts.Fundamentals;

public sealed record ImportFundamentalsRequest(
    Guid FibraId,
    string Period,
    decimal? CapRate,
    decimal? NavPerCbfi,
    decimal? Ltv,
    decimal? NoiMargin,
    decimal? FfoMargin,
    decimal? QuarterlyDistribution,
    string? Summary,
    string? PdfReference,
    IReadOnlyDictionary<string, string>? FieldNotes = null);
