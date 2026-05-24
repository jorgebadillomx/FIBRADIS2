using Application.Jobs;
using Domain.Jobs;
using SharedApiContracts.Jobs;

namespace Api.Endpoints.Ops;

public static class OpsDashboardEndpoints
{
    private static readonly string[] Pipelines = ["Market", "News", "Distribution"];

    public static IEndpointRouteBuilder MapOpsDashboard(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ops/dashboard")
            .RequireAuthorization("AdminOps")
            .WithTags("Ops");

        group.MapGet("/", async (
            IPipelineRunLogRepository runLogRepo,
            IPipelineErrorLogRepository errorLogRepo,
            CancellationToken ct) =>
        {
            var pipelineStatuses = new List<PipelineStatusDto>(Pipelines.Length);

            foreach (var pipeline in Pipelines)
            {
                var lastCompleted = await runLogRepo.GetLastCompletedAsync(pipeline, ct);
                var recentRuns = await runLogRepo.GetRecentAsync(pipeline, 5, ct);

                pipelineStatuses.Add(new PipelineStatusDto(
                    pipeline,
                    DeriveStatus(lastCompleted),
                    lastCompleted?.CompletedAt,
                    CalculateDurationSeconds(lastCompleted),
                    lastCompleted?.ItemsProcessed,
                    lastCompleted?.ErrorCount,
                    recentRuns.Select(MapRunDto).ToList()));
            }

            var (errorItems, _) = await errorLogRepo.GetPagedAsync(null, 1, 5, ct);
            var recentErrors = errorItems
                .Select(item => new PipelineErrorLogDto(
                    item.Id,
                    item.Pipeline,
                    item.Timestamp,
                    item.ErrorType,
                    item.Message,
                    item.Context,
                    item.AiContext,
                    item.CreatedAt))
                .ToList();

            return Results.Ok(new PipelineDashboardDto(pipelineStatuses, recentErrors));
        })
        .Produces<PipelineDashboardDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }

    private static string DeriveStatus(PipelineRunLog? lastCompleted)
    {
        if (lastCompleted is null)
            return "Sin datos";

        return string.Equals(lastCompleted.Status, "Failed", StringComparison.OrdinalIgnoreCase)
               || (lastCompleted.ErrorCount ?? 0) > 0
            ? "Fallando"
            : "Completado";
    }

    private static int? CalculateDurationSeconds(PipelineRunLog? lastCompleted)
    {
        if (lastCompleted?.CompletedAt is null)
            return null;

        return (int)Math.Max(0, Math.Round((lastCompleted.CompletedAt.Value - lastCompleted.StartedAt).TotalSeconds));
    }

    private static PipelineRunLogDto MapRunDto(PipelineRunLog item)
        => new(
            item.Id,
            item.Pipeline,
            item.StartedAt,
            item.CompletedAt,
            item.Status,
            item.ItemsProcessed,
            item.ErrorCount,
            item.TriggeredBy,
            item.Details);
}
