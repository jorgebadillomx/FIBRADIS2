using Application.Opportunities;
using Domain.Portfolio;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Opportunities;

public class OpportunityWeightsRepository(AppDbContext db) : IOpportunityWeightsRepository
{
    public async Task<UserOpportunityWeights?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await db.UserOpportunityWeights
            .FirstOrDefaultAsync(w => w.UserId == userId, ct);

    public async Task UpsertAsync(UserOpportunityWeights weights, CancellationToken ct = default)
    {
        var existing = await db.UserOpportunityWeights
            .FirstOrDefaultAsync(w => w.UserId == weights.UserId, ct);

        if (existing is not null)
        {
            existing.WeightsJson = weights.WeightsJson;
            existing.Profile = weights.Profile;
            existing.UpdatedAt = weights.UpdatedAt;
            await db.SaveChangesAsync(ct);
            return;
        }

        db.UserOpportunityWeights.Add(weights);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            db.Entry(weights).State = EntityState.Detached;
            var retried = await db.UserOpportunityWeights
                .FirstOrDefaultAsync(w => w.UserId == weights.UserId, ct);
            if (retried is not null)
            {
                retried.WeightsJson = weights.WeightsJson;
                retried.Profile = weights.Profile;
                retried.UpdatedAt = weights.UpdatedAt;
                await db.SaveChangesAsync(ct);
            }
        }
    }
}
