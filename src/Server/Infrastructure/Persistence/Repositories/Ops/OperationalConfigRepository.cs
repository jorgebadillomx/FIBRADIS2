using System.Globalization;
using Application.Ops;
using Domain.Ops;
using Infrastructure.Persistence.SqlServer;

namespace Infrastructure.Persistence.Repositories.Ops;

public class OperationalConfigRepository(AppDbContext db) : IOperationalConfigRepository
{
    public async Task<OperationalConfig> GetAsync(CancellationToken ct = default)
        => await db.OperationalConfigs.FindAsync([1], ct)
           ?? new OperationalConfig();

    public async Task UpdateAsync(
        decimal? commissionFactor,
        int? avgPeriods,
        int? newsCadenceMinutes,
        int? fibraNewsMonths,
        int? fundamentalsCadenceMinutes,
        int? distributionCadenceMinutes,
        string actor,
        CancellationToken ct = default)
    {
        var config = await db.OperationalConfigs.FindAsync([1], ct);
        if (config is null)
        {
            config = new OperationalConfig { Id = 1 };
            db.OperationalConfigs.Add(config);
        }

        var auditEntries = new List<ConfigAuditLog>();
        var now = DateTimeOffset.UtcNow;

        if (commissionFactor.HasValue && config.CommissionFactor != commissionFactor.Value)
        {
            auditEntries.Add(new ConfigAuditLog
            {
                Actor = actor,
                ChangedAt = now,
                FieldName = "commission_factor",
                PreviousValue = config.CommissionFactor.ToString("0.######", CultureInfo.InvariantCulture),
                NewValue = commissionFactor.Value.ToString("0.######", CultureInfo.InvariantCulture),
            });
            config.CommissionFactor = commissionFactor.Value;
        }

        if (avgPeriods.HasValue && config.AvgPeriods != avgPeriods.Value)
        {
            auditEntries.Add(new ConfigAuditLog
            {
                Actor = actor,
                ChangedAt = now,
                FieldName = "avg_periods",
                PreviousValue = config.AvgPeriods.ToString(CultureInfo.InvariantCulture),
                NewValue = avgPeriods.Value.ToString(CultureInfo.InvariantCulture),
            });
            config.AvgPeriods = avgPeriods.Value;
        }

        if (newsCadenceMinutes.HasValue && config.NewsCadenceMinutes != newsCadenceMinutes.Value)
        {
            auditEntries.Add(new ConfigAuditLog
            {
                Actor = actor,
                ChangedAt = now,
                FieldName = "news_cadence_minutes",
                PreviousValue = config.NewsCadenceMinutes.ToString(CultureInfo.InvariantCulture),
                NewValue = newsCadenceMinutes.Value.ToString(CultureInfo.InvariantCulture),
            });
            config.NewsCadenceMinutes = newsCadenceMinutes.Value;
        }

        if (fibraNewsMonths.HasValue && config.FibraNewsMonths != fibraNewsMonths.Value)
        {
            auditEntries.Add(new ConfigAuditLog
            {
                Actor = actor,
                ChangedAt = now,
                FieldName = "fibra_news_months",
                PreviousValue = config.FibraNewsMonths.ToString(CultureInfo.InvariantCulture),
                NewValue = fibraNewsMonths.Value.ToString(CultureInfo.InvariantCulture),
            });
            config.FibraNewsMonths = fibraNewsMonths.Value;
        }

        if (fundamentalsCadenceMinutes.HasValue && config.FundamentalsCadenceMinutes != fundamentalsCadenceMinutes.Value)
        {
            auditEntries.Add(new ConfigAuditLog
            {
                Actor = actor,
                ChangedAt = now,
                FieldName = "fundamentals_cadence_minutes",
                PreviousValue = config.FundamentalsCadenceMinutes.ToString(CultureInfo.InvariantCulture),
                NewValue = fundamentalsCadenceMinutes.Value.ToString(CultureInfo.InvariantCulture),
            });
            config.FundamentalsCadenceMinutes = fundamentalsCadenceMinutes.Value;
        }

        if (distributionCadenceMinutes.HasValue && config.DistributionCadenceMinutes != distributionCadenceMinutes.Value)
        {
            auditEntries.Add(new ConfigAuditLog
            {
                Actor = actor,
                ChangedAt = now,
                FieldName = "distribution_cadence_minutes",
                PreviousValue = config.DistributionCadenceMinutes.ToString(CultureInfo.InvariantCulture),
                NewValue = distributionCadenceMinutes.Value.ToString(CultureInfo.InvariantCulture),
            });
            config.DistributionCadenceMinutes = distributionCadenceMinutes.Value;
        }

        if (auditEntries.Count == 0)
        {
            return;
        }

        config.UpdatedAt = now;
        config.UpdatedBy = actor;
        db.ConfigAuditLogs.AddRange(auditEntries);
        await db.SaveChangesAsync(ct);
    }
}
