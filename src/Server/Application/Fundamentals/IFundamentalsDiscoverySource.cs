using Domain.Catalog;

namespace Application.Fundamentals;

public interface IFundamentalsDiscoverySource
{
    string SourceName { get; }
    IReadOnlyList<string> SupportedTickers { get; }
    Task<IReadOnlyList<FundamentalsDiscoveryCandidate>> DiscoverCandidatesAsync(Fibra fibra, CancellationToken ct);
}
