using System.Text.Json;
using Application.Auth;
using Application.Email;
using Application.Jobs;
using Domain.Auth;
using Domain.Jobs;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Jobs.Subscriptions;

[DisableConcurrentExecution(timeoutInSeconds: 0)]
public class SubscriptionMaintenanceJob(
    IUserService userService,
    IEmailService emailService,
    IPipelineRunLogRepository runLogRepo,
    IPipelineErrorLogRepository errorLogRepo,
    ILogger<SubscriptionMaintenanceJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var status = "Failed";
        var deactivated = 0;
        var notified = 0;
        var errors = 0;

        try
        {
            var usersToDeactivate = await userService.FindUsersToDeactivateAsync(ct);
            if (usersToDeactivate.Count > 0)
            {
                await userService.BulkDeactivateUsersAsync(usersToDeactivate.Select(u => u.Id).ToList(), ct);
                deactivated = usersToDeactivate.Count;

                foreach (var user in usersToDeactivate)
                {
                    try
                    {
                        await emailService.SendAccessExpiredAsync(user.Email, ct);
                        notified++;
                    }
                    catch (Exception ex)
                    {
                        errors++;
                        logger.LogError(ex, "No se pudo enviar el aviso de acceso expirado para userId={UserId}", user.Id);
                        await TryLogErrorAsync($"Email expiración userId={user.Id}", ex);
                    }
                }
            }

            var expiringTrials = await userService.FindUsersWithExpiringTrialAsync(3, ct);
            foreach (var user in expiringTrials)
            {
                try
                {
                    await emailService.SendTrialExpiringAsync(user.Email, 3, ct);
                    notified++;
                }
                catch (Exception ex)
                {
                    errors++;
                    logger.LogError(ex, "No se pudo enviar el aviso de trial para userId={UserId}", user.Id);
                    await TryLogErrorAsync($"Email aviso trial userId={user.Id}", ex);
                }
            }

            var expiringMonthly = await userService.FindUsersWithExpiringSubscriptionAsync(3, SubscriptionType.Monthly, ct);
            foreach (var user in expiringMonthly)
            {
                try
                {
                    await emailService.SendSubscriptionExpiringAsync(user.Email, 3, ct);
                    notified++;
                }
                catch (Exception ex)
                {
                    errors++;
                    logger.LogError(ex, "No se pudo enviar el aviso de suscripción Monthly para userId={UserId}", user.Id);
                    await TryLogErrorAsync($"Email aviso Monthly userId={user.Id}", ex);
                }
            }

            var expiringAnnual = await userService.FindUsersWithExpiringSubscriptionAsync(30, SubscriptionType.Annual, ct);
            foreach (var user in expiringAnnual)
            {
                try
                {
                    await emailService.SendSubscriptionExpiringAsync(user.Email, 30, ct);
                    notified++;
                }
                catch (Exception ex)
                {
                    errors++;
                    logger.LogError(ex, "No se pudo enviar el aviso de suscripción Annual para userId={UserId}", user.Id);
                    await TryLogErrorAsync($"Email aviso Annual userId={user.Id}", ex);
                }
            }

            status = "Completed";
        }
        catch (OperationCanceledException)
        {
            status = "Cancelled";
            logger.LogWarning("SubscriptionMaintenanceJob cancelado");
        }
        catch (Exception ex)
        {
            errors++;
            logger.LogError(ex, "SubscriptionMaintenanceJob: error inesperado");
            await TryLogErrorAsync("Error inesperado en SubscriptionMaintenanceJob", ex);
        }
        finally
        {
            await TryLogRunAsync(startedAt, status, deactivated + notified, errors);
        }
    }

    private async Task TryLogErrorAsync(string context, Exception ex)
    {
        try
        {
            var errorType = ex.GetType().Name;
            var message = ex.Message;

            await errorLogRepo.LogErrorAsync(new PipelineErrorLog
            {
                Pipeline = "SubscriptionMaintenance",
                Timestamp = DateTimeOffset.UtcNow,
                ErrorType = Truncate(errorType, 100),
                Message = Truncate(message, 500),
                Context = context,
                AiContext = Truncate(
                    $"SubscriptionMaintenanceJob falló en: {context}. Error: {message}. Verificar conectividad con Resend o estado de BD.",
                    800),
            }, CancellationToken.None);
        }
        catch (Exception logEx)
        {
            logger.LogWarning(logEx, "SubscriptionMaintenanceJob: fallo al escribir PipelineErrorLog");
        }
    }

    private async Task TryLogRunAsync(DateTimeOffset startedAt, string status, int processed, int errors)
    {
        try
        {
            await runLogRepo.AddAsync(new PipelineRunLog
            {
                Pipeline = "SubscriptionMaintenance",
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
            logger.LogWarning(ex, "SubscriptionMaintenanceJob: fallo al escribir PipelineRunLog");
        }
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
