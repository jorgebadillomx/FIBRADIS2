namespace SharedApiContracts.Opportunities;

public sealed record OpportunityRankingResponseDto(
    IReadOnlyList<OpportunityFibraRowDto> Ranked,
    IReadOnlyList<OpportunityFibraRowDto> LimitedData,
    OpportunityWeightsDto Weights,
    UniverseCoverageDto Coverage
);
