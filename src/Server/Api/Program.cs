using Api.CompositionRoot;
using Api.Endpoints.Public;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiInfrastructure();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.UseApiInfrastructure();
app.UseHttpsRedirection();
app.MapHealth();

app.Run();

public partial class Program { }
