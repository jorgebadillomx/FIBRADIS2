using Application.Jobs;
using SharedApiContracts.Common;
using SharedApiContracts.Jobs;

namespace Api.Endpoints.Ops;

public static class OpsPipelineLogEndpoints
{
    private static readonly string[] AllowedPipelines = ["Market", "News", "Distribution", "BodyTextRetry", "ManualAiSummary", "KpiExtraction"];

    public static IEndpointRouteBuilder MapOpsPipelineLogs(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ops/pipeline-logs")
            .RequireAuthorization("AdminOps")
            .WithTags("Ops");

        group.MapGet("/", async (
            IPipelineErrorLogRepository repo,
            string? pipeline,
            CancellationToken ct,
            int page = 1,
            int pageSize = 50) =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var normalizedPipeline = string.Equals(pipeline, "all", StringComparison.OrdinalIgnoreCase)
                ? null
                : AllowedPipelines.FirstOrDefault(p => string.Equals(p, pipeline, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(pipeline)
                && !string.Equals(pipeline, "all", StringComparison.OrdinalIgnoreCase)
                && normalizedPipeline is null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["pipeline"] = ["Valor inválido. Use: all, Market, News, Distribution o BodyTextRetry."],
                });
            }

            var (items, total) = await repo.GetPagedAsync(normalizedPipeline, page, pageSize, ct);
            var dtos = items.Select(item => new PipelineErrorLogDto(
                item.Id,
                item.Pipeline,
                item.Timestamp,
                item.ErrorType,
                item.Message,
                item.Context,
                item.AiContext,
                item.CreatedAt)).ToList();

            return Results.Ok(new PagedResult<PipelineErrorLogDto>(dtos, page, pageSize, total));
        })
        .Produces<PagedResult<PipelineErrorLogDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }
}
