namespace SharedApiContracts.Portfolio;

public sealed record PortfolioUploadResponseDto(
    int PositionCount
)
{
    public bool DuplicateDetected { get; init; } = false;
}
