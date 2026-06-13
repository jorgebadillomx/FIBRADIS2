using System.Text.Json;
using Application.Integrations;
using Application.Ops;
using Domain.Jobs;
using Microsoft.Extensions.Logging;

namespace Application.Jobs;

public class BanxicoSyncJob(
    IBanxicoClient banxico,
    IOperationalConfigRepository config,
    IPipelineRunLogRepository runLogRepo,
    IPipelineErrorLogRepository errorLogRepo,
    ILogger<BanxicoSyncJob> logger)
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

            var cetes = await banxico.GetCetes28dAsync(ct);
            if (cetes is null)
            {
                logger.LogWarning("BanxicoSyncJob: no se obtuvo tasa CETES 28d");
                errors++;
                await TryLogErrorAsync("CETES 28d (SF43936) retornó null — Banxico no devolvió dato válido.");
            }
            else
            {
                await config.UpdateCetesRateAsync(cetes.Value, now, ct);
                processed++;
            }

            var tiie = await banxico.GetTiie28dAsync(ct);
            if (tiie is null)
            {
                logger.LogWarning("BanxicoSyncJob: no se obtuvo tasa TIIE 28d");
                errors++;
                await TryLogErrorAsync("TIIE 28d (SF60542) retornó null — Banxico no devolvió dato válido.");
            }
            else
            {
                await config.UpdateTiieRateAsync(tiie.Value, now, ct);
                processed++;
            }

            status = "Completed";
            logger.LogInformation("BanxicoSyncJob: CETES={Cetes} TIIE={Tiie}", cetes, tiie);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "BanxicoSyncJob: error inesperado");
            errors++;
            await TryLogErrorAsync($"Error inesperado en BanxicoSyncJob: {ex.Message}", ex);
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
                Pipeline = "BanxicoSync",
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
            logger.LogWarning(ex, "BanxicoSyncJob: fallo al escribir PipelineRunLog");
        }
    }

    private async Task TryLogErrorAsync(string message, Exception? ex = null)
    {
        try
        {
            await errorLogRepo.LogErrorAsync(new PipelineErrorLog
            {
                Pipeline = "BanxicoSync",
                Timestamp = DateTimeOffset.UtcNow,
                ErrorType = ex?.GetType().Name ?? "NullResult",
                Message = message.Length > 500 ? message[..500] : message,
                AiContext = $"BanxicoSyncJob intentó sincronizar tasas CETES y TIIE 28d desde la API de Banxico. {message} Revise Banxico:Token en appsettings y la disponibilidad del endpoint SIE.",
            }, CancellationToken.None);
        }
        catch (Exception logEx)
        {
            logger.LogWarning(logEx, "BanxicoSyncJob: fallo al escribir PipelineErrorLog");
        }
    }
}
