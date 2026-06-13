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
        var now = DateTimeOffset.UtcNow;

        var cetes = await banxico.GetCetes28dAsync(ct);
        if (cetes is null)
        {
            logger.LogWarning("BanxicoSyncJob: no se obtuvo tasa CETES 28d");
        }
        else
        {
            await config.UpdateCetesRateAsync(cetes.Value, now, ct);
        }

        var tiie = await banxico.GetTiie28dAsync(ct);
        if (tiie is null)
        {
            logger.LogWarning("BanxicoSyncJob: no se obtuvo tasa TIIE 28d");
        }
        else
        {
            await config.UpdateTiieRateAsync(tiie.Value, now, ct);
        }

        logger.LogInformation("BanxicoSyncJob: CETES={Cetes} TIIE={Tiie}", cetes, tiie);
    }
}
