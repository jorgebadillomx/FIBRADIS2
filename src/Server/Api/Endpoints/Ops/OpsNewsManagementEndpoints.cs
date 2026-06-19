using Application.News;
using Hangfire;
using Infrastructure.Jobs.News;
using SharedApiContracts.News;

namespace Api.Endpoints.Ops;

public static class OpsNewsManagementEndpoints
{
    public static IEndpointRouteBuilder MapOpsNewsManagement(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ops/news")
            .RequireAuthorization("AdminOps")
            .WithTags("OpsNewsManagement");

        // Idempotente: si se invoca dos veces, la segunda pasada no encuentra artículos sin slug.
        group.MapPost("/backfill-slugs", async (
            INewsRepository newsRepo,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            const int BatchSize = 100;
            var logger = loggerFactory.CreateLogger("OpsNewsManagement");
            var count = 0;
            // Un artículo que falla (p.ej. colisión de slug con otra ejecución concurrente)
            // volvería en el siguiente batch — saltarlo evita un loop infinito y permite
            // devolver el conteo parcial en vez de 500 a mitad del backfill.
            var failedIds = new HashSet<Guid>();
            IReadOnlyList<Domain.News.NewsArticle> batch;
            do
            {
                batch = await newsRepo.GetArticlesWithoutSlugAsync(BatchSize, ct);
                var pending = batch.Where(a => !failedIds.Contains(a.Id)).ToList();
                if (pending.Count == 0)
                    break;

                foreach (var article in pending)
                {
                    try
                    {
                        var slug = await newsRepo.GenerateUniqueSlugAsync(article.Title, article.Id, ct);
                        await newsRepo.UpdateSlugAsync(article.Id, slug, ct);
                        count++;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        failedIds.Add(article.Id);
                        logger.LogWarning(ex, "Backfill de slug falló para el artículo {ArticleId}; se omite", article.Id);
                    }
                }
            } while (batch.Count == BatchSize);

            if (failedIds.Count > 0)
                logger.LogWarning("Backfill de slugs completó con {Failed} artículos omitidos por error", failedIds.Count);

            return Results.Ok(new BackfillSlugsResultDto(count));
        })
        .Produces<BackfillSlugsResultDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/trigger-pipeline", (
            TriggerPipelineRequestDto req,
            IBackgroundJobClient jobClient) =>
        {
            if (req.FibraIds is { Length: > 0 })
                jobClient.Enqueue<NewsPipelineJob>(j => j.ExecuteForFibrasAsync(req.FibraIds, CancellationToken.None));
            else
                jobClient.Enqueue<NewsPipelineJob>(j => j.ExecuteAsync(CancellationToken.None));

            return Results.Accepted();
        })
        .Produces(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }
}
