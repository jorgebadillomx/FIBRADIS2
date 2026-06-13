using Application.Jobs;
using Hangfire;

namespace Api.Endpoints.Ops;

public static class OpsBanxicoEndpoints
{
    public static IEndpointRouteBuilder MapOpsBanxico(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ops/banxico")
            .RequireAuthorization("AdminOps")
            .WithTags("Ops");

        group.MapPost("/sync-tiie/run", (IBackgroundJobClient jobs) =>
        {
            jobs.Enqueue<BanxicoSyncJob>(j => j.ExecuteAsync(CancellationToken.None));
            return Results.Accepted();
        })
        .Produces(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/sync-inpc/run", (IBackgroundJobClient jobs) =>
        {
            jobs.Enqueue<BanxicoMonthlySyncJob>(j => j.ExecuteAsync(CancellationToken.None));
            return Results.Accepted();
        })
        .Produces(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }
}
