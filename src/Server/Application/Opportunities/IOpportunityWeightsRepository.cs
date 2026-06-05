using Domain.Portfolio;

namespace Application.Opportunities;

public interface IOpportunityWeightsRepository
{
    Task<UserOpportunityWeights?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task UpsertAsync(UserOpportunityWeights weights, CancellationToken ct = default);
}
