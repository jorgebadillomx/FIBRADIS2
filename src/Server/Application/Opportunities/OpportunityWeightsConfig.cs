namespace Application.Opportunities;

public sealed record OpportunityWeights(
    decimal NavDiscount,
    decimal DividendYield,
    decimal LtvInverted,
    decimal NoiMargin,
    decimal Pricevs52w,
    string Profile)
{
    public static readonly OpportunityWeights Default = new(30m, 30m, 20m, 10m, 10m, "default");
    public static readonly OpportunityWeights Balanced = new(20m, 20m, 20m, 20m, 20m, "balanceado");
    public static readonly OpportunityWeights Renta = new(20m, 50m, 10m, 20m, 0m, "renta");
    public static readonly OpportunityWeights Crecimiento = new(40m, 15m, 25m, 10m, 10m, "crecimiento");

    public static OpportunityWeights FromProfile(string profile) => profile switch
    {
        "renta" => Renta,
        "crecimiento" => Crecimiento,
        _ => Default,
    };
}
