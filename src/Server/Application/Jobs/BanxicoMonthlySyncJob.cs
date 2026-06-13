using Application.Integrations;
using Application.Ops;
using Domain.Ops;
using Microsoft.Extensions.Logging;

namespace Application.Jobs;

public class BanxicoMonthlySyncJob(
    IBanxicoClient banxico,
    IInpcRepository inpcRepo,
    ILogger<BanxicoMonthlySyncJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var todayPeriod = new DateOnly(now.Year, now.Month, 1);
        var lastPeriodo = await inpcRepo.GetLatestPeriodoAsync(ct);
        DateOnly from;

        if (lastPeriodo is null)
        {
            from = todayPeriod.AddMonths(-25);
            logger.LogInformation("BanxicoMonthlySyncJob: primera corrida, desde {From}", from);
        }
        else
        {
            from = lastPeriodo.Value.AddMonths(1);
            if (from > todayPeriod)
            {
                logger.LogInformation("BanxicoMonthlySyncJob: ya al día, nada que sincronizar");
                return;
            }

            logger.LogInformation("BanxicoMonthlySyncJob: catch-up desde {From}", from);
        }

        var history = await banxico.GetInpcHistoryAsync(from, today, ct);
        if (history.Count == 0)
        {
            logger.LogWarning("BanxicoMonthlySyncJob: Banxico no retornó datos INPC para el rango [{From}, {To}]", from, today);
            return;
        }

        var entries = history.Select(h => new InpcMonthlyEntry
        {
            Periodo = new DateOnly(h.Periodo.Year, h.Periodo.Month, 1),
            InpcIndex = h.InpcIndex,
            CapturedAt = now,
        }).ToList();

        await inpcRepo.UpsertManyAsync(entries, ct);
        logger.LogInformation("BanxicoMonthlySyncJob: {Count} registros INPC upsertados", entries.Count);
    }
}
