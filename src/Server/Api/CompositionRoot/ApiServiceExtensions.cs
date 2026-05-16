namespace Api.CompositionRoot;

public static class ApiServiceExtensions
{
    public static WebApplicationBuilder AddApiInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenApi("v1");

        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = ctx =>
            {
                ctx.ProblemDetails.Extensions["correlationId"] =
                    ctx.HttpContext.Items["CorrelationId"]?.ToString()
                    ?? ctx.HttpContext.TraceIdentifier;

                if (!ctx.ProblemDetails.Extensions.ContainsKey("domainCode"))
                    ctx.ProblemDetails.Extensions["domainCode"] = null;
            };
        });

        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddCors(options =>
                options.AddPolicy("SpaDev", policy =>
                    policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials()));
        }

        return builder;
    }
}
