namespace SharedApiContracts.Opportunities;

public sealed record OpportunityWeightsDto(
    decimal NavDiscount,
    decimal DividendYield,
    decimal LtvInverted,
    decimal NoiMargin,
    decimal Pricevs52w,
    string Profile
);
