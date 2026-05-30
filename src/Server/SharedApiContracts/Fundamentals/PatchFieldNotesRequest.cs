namespace SharedApiContracts.Fundamentals;

public sealed record PatchFieldNotesRequest(
    string? CapRateNote,
    string? NavPerCbfiNote,
    string? LtvNote,
    string? NoiMarginNote,
    string? FfoMarginNote,
    string? QuarterlyDistributionNote);
