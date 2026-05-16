namespace Api.Endpoints.Public;

public static class HealthEndpoint
{
    public static IEndpointRouteBuilder MapHealth(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/v1")
           .MapGet("/health", () => Results.Ok(new { status = "healthy" }))
           .WithName("GetHealth")
           .WithTags("Health")
           .Produces(StatusCodes.Status200OK);
        return app;
    }
}
