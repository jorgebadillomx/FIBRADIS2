using Application.Ai;
using SharedApiContracts.Ai;
using SharedApiContracts.Common;

namespace Api.Endpoints.Ops;

public static class OpsAiCallLogEndpoints
{
    public static IEndpointRouteBuilder MapOpsAiCallLogs(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ops/ai-call-logs")
            .RequireAuthorization("AdminOps")
            .WithTags("Ops");

        group.MapGet("/", async (
            IAiCallLogRepository repo,
            string? operation,
            CancellationToken ct,
            int page = 1,
            int pageSize = 50) =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var normalizedOp = string.IsNullOrWhiteSpace(operation) || string.Equals(operation, "all", StringComparison.OrdinalIgnoreCase)
                ? null
                : operation;

            var (items, total) = await repo.GetPagedAsync(normalizedOp, page, pageSize, ct);
            var dtos = items.Select(item => new AiCallLogDto(
                item.Id,
                item.Timestamp,
                item.Operation,
                item.Provider,
                item.ModelId,
                item.PromptLength,
                item.DurationMs,
                item.Success,
                item.RequestRaw,
                item.ResponseRaw,
                item.ErrorMessage,
                item.Context,
                item.CreatedAt)).ToList();

            return Results.Ok(new PagedResult<AiCallLogDto>(dtos, page, pageSize, total));
        })
        .Produces<PagedResult<AiCallLogDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }
}
