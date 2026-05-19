using Api.CompositionRoot;
using Api.Endpoints.Ops;
using Api.Endpoints.Private;
using Api.Endpoints.Public;
using Hangfire;
using Infrastructure.Jobs.Market;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiInfrastructure();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.UseApiInfrastructure();
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapAuth();
app.MapMe();
app.MapOpsPing();
app.MapCatalog();

app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

var useInMemoryHangfire = builder.Configuration.GetValue<bool>("Hangfire:UseInMemoryStorage");
var hangfireConnStr = builder.Configuration.GetConnectionString("DefaultConnection");
if (!useInMemoryHangfire && !string.IsNullOrEmpty(hangfireConnStr))
{
    RecurringJob.AddOrUpdate<MarketPipelineJob>(
        "market-pipeline",
        j => j.ExecuteAsync(CancellationToken.None),
        "*/15 * * * 1-5",
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
}

app.Run();

public partial class Program { }
