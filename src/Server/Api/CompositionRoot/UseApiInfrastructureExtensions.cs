using Api.Hangfire;
using Api.HealthChecks;
using Api.Middleware;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
            app.UseCors("SpaDev");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = JsonHealthCheckResponseWriter.Write,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status200OK,
            },
        })
        .WithMetadata(new ProducesResponseTypeMetadata(
            StatusCodes.Status200OK,
            typeof(HealthCheckResponse),
            ["application/json"]));

        // Dashboard solo si Hangfire está configurado con storage (no en modo test/inMemory)
        var useInMemoryHangfire = app.Configuration.GetValue<bool>("Hangfire:UseInMemoryStorage");
        var hangfireConnStr = app.Configuration.GetConnectionString("DefaultConnection");
        if (!useInMemoryHangfire && !string.IsNullOrEmpty(hangfireConnStr))
        {
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = [new HangfireDashboardAuthFilter(
                    app.Services.GetRequiredService<IWebHostEnvironment>())],
            });
        }

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference("/swagger", options =>
            {
                options.WithTitle("FIBRADIS API");
            });
        }

        return app;
    }
}
