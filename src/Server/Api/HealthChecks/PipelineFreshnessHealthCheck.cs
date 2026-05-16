using Hangfire;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Api.HealthChecks;

public class PipelineFreshnessHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var storage = JobStorage.Current;
            var api = storage.GetMonitoringApi();
            var failedCount = api.FailedCount();

            return failedCount > 0
                ? Task.FromResult(HealthCheckResult.Degraded(
                    $"{failedCount} job(s) fallidos sin reintentar en la cola"))
                : Task.FromResult(HealthCheckResult.Healthy());
        }
        catch (InvalidOperationException)
        {
            // JobStorage.Current lanza si Hangfire no tiene storage configurado (tests)
            return Task.FromResult(HealthCheckResult.Healthy("Sin storage de Hangfire configurado"));
        }
    }
}
