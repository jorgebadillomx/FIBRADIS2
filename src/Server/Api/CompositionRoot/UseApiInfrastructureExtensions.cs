using Api.Middleware;
using Scalar.AspNetCore;

namespace Api.CompositionRoot;

public static class UseApiInfrastructureExtensions
{
    public static WebApplication UseApiInfrastructure(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (app.Environment.IsDevelopment())
        {
            app.UseCors("SpaDev");
            app.MapOpenApi();
            app.MapScalarApiReference("/swagger", options =>
            {
                options.WithTitle("FIBRADIS API");
            });
        }

        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
