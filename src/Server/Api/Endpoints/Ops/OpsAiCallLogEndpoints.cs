using Application.Ai;
using SharedApiContracts.Ai;
using SharedApiContracts.Common;

namespace Api.Endpoints.Ops;

public static class OpsAiCallLogEndpoints
{
    private static readonly string[] AllowedProviders = ["Gemini", "DeepSeek"];
    private static readonly string[] AllowedOperations = ["NewsSummary", "KpiExtraction", "News", "Document"];

    public static IEndpointRouteBuilder MapOpsAiCallLogs(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ops/ai-call-logs")
            .RequireAuthorization("AdminOps")
            .WithTags("Ops");

        group.MapGet("/", async (
            IAiCallLogRepository repo,
            string? operation,
            string? provider,
            bool? success,
            CancellationToken ct,
            int page = 1,
            int pageSize = 50) =>
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            string? normalizedOp = null;
            if (!string.IsNullOrWhiteSpace(operation) && !string.Equals(operation, "all", StringComparison.OrdinalIgnoreCase))
            {
                normalizedOp = AllowedOperations.FirstOrDefault(o => string.Equals(o, operation, StringComparison.OrdinalIgnoreCase));
                if (normalizedOp is null)
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["operation"] = [$"Operación no reconocida. Valores permitidos: {string.Join(", ", AllowedOperations)}."],
                    });
            }

            string? normalizedProvider = null;
            if (!string.IsNullOrWhiteSpace(provider) && !string.Equals(provider, "all", StringComparison.OrdinalIgnoreCase))
            {
                normalizedProvider = AllowedProviders.FirstOrDefault(p => string.Equals(p, provider, StringComparison.OrdinalIgnoreCase));
                if (normalizedProvider is null)
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["provider"] = [$"Proveedor no reconocido. Valores permitidos: {string.Join(", ", AllowedProviders)}."],
                    });
            }

            var (items, total) = await repo.GetPagedAsync(normalizedOp, normalizedProvider, success, page, pageSize, ct);
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
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }
}
