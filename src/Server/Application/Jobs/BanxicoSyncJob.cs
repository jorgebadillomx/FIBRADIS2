using Application.Integrations;
using Application.Ops;
using Microsoft.Extensions.Logging;

namespace Application.Jobs;

public class BanxicoSyncJob(
    IBanxicoClient banxico,
    IOperationalConfigRepository config,
    ILogger<BanxicoSyncJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var rate = await banxico.GetCetes28dAsync(ct);
        if (rate is null)
        {
            logger.LogWarning("BanxicoSyncJob: no se obtuvo tasa CETES");
            return;
        }

        try
        {
            await config.UpdateCetesRateAsync(rate.Value, DateTimeOffset.UtcNow, ct);
            logger.LogInformation("BanxicoSyncJob: CETES 28d actualizado a {Rate}", rate);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BanxicoSyncJob: error guardando tasa CETES");
        }
    }
}
