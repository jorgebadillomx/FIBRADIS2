namespace SharedApiContracts.Portfolio;

public sealed record PortfolioSnapshotStatusDto(
    bool HasSnapshot,
    DateTimeOffset? ArchivedAt
);
