namespace Api.Endpoints.Ops;

public static class OpsEndpoints
{
    public static IEndpointRouteBuilder MapOpsPing(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/ops/ping", () =>
            Results.Ok(new { status = "ok" }))
           .RequireAuthorization("AdminOps")
           .WithTags("Ops")
           .Produces<object>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status401Unauthorized)
           .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }
}
