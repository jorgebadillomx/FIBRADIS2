using System.Text.Json;
using Application.Integrations;
using Application.Ops;
using Domain.Jobs;
using Domain.Ops;
using Microsoft.Extensions.Logging;

namespace Application.Jobs;

public class BanxicoMonthlySyncJob(
    IBanxicoClient banxico,
    IInpcRepository inpcRepo,
    IPipelineRunLogRepository runLogRepo,
    IPipelineErrorLogRepository errorLogRepo,
    ILogger<BanxicoMonthlySyncJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var status = "Failed";
        var processed = 0;
        var errors = 0;

        try
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
                    status = "Completed";
                    return;
                }

                logger.LogInformation("BanxicoMonthlySyncJob: catch-up desde {From}", from);
            }

            var history = await banxico.GetInpcHistoryAsync(from, today, ct);
            if (history.Count == 0)
            {
                logger.LogWarning("BanxicoMonthlySyncJob: Banxico no retornó datos INPC para el rango [{From}, {To}]", from, today);
                errors = 1;
                await TryLogErrorAsync($"Banxico no retornó datos INPC para el rango [{from}, {today}]. Serie SP1.");
                status = "Completed";
                return;
            }

            var entries = history.Select(h => new InpcMonthlyEntry
            {
                Periodo = new DateOnly(h.Periodo.Year, h.Periodo.Month, 1),
                InpcIndex = h.InpcIndex,
                CapturedAt = now,
            }).ToList();

            await inpcRepo.UpsertManyAsync(entries, ct);
            processed = entries.Count;
            status = "Completed";
            logger.LogInformation("BanxicoMonthlySyncJob: {Count} registros INPC upsertados", entries.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BanxicoMonthlySyncJob: error inesperado");
            errors = 1;
            await TryLogErrorAsync($"Error inesperado en BanxicoMonthlySyncJob: {ex.Message}", ex);
        }
        finally
        {
            await TryLogRunAsync(startedAt, status, processed, errors);
        }
    }

    private async Task TryLogRunAsync(DateTimeOffset startedAt, string status, int processed, int errors)
    {
        try
        {
            await runLogRepo.AddAsync(new PipelineRunLog
            {
                Pipeline = "BanxicoInpc",
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow,
                Status = status,
                ItemsProcessed = processed,
                ErrorCount = errors,
                Details = JsonSerializer.Serialize(new { processed, errors }),
            }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "BanxicoMonthlySyncJob: fallo al escribir PipelineRunLog");
        }
    }

    private async Task TryLogErrorAsync(string message, Exception? ex = null)
    {
        try
        {
            await errorLogRepo.LogErrorAsync(new PipelineErrorLog
            {
                Pipeline = "BanxicoInpc",
                Timestamp = DateTimeOffset.UtcNow,
                ErrorType = ex?.GetType().Name ?? "EmptyResult",
                Message = message.Length > 500 ? message[..500] : message,
                AiContext = $"BanxicoMonthlySyncJob intentó sincronizar el INPC mensual (serie SP1) desde la API de Banxico. {message} Revise el token de API y la disponibilidad del endpoint SIE para la serie SP1.",
            }, CancellationToken.None);
        }
        catch (Exception logEx)
        {
            logger.LogWarning(logEx, "BanxicoMonthlySyncJob: fallo al escribir PipelineErrorLog");
        }
    }
}
