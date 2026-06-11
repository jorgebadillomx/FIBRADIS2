namespace Infrastructure.Jobs.Market;

public sealed record MasDividendosImportResult(int Updated, int Skipped, int Unmatched);
