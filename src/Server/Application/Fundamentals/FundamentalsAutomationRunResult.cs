namespace Application.Fundamentals;

public sealed record FundamentalsAutomationRunResult(
    int FibrasScanned,
    int ReportsDetected,
    int NewReports,
    int SkippedReports,
    int PossibleUpdates,
    int AnnualReports,
    int AmbiguousReports,
    int Errors,
    int RecordsProcessed);
