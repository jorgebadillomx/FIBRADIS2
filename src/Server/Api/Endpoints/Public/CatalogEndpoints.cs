using Application.Catalog;
using SharedApiContracts.Catalog;
using SharedApiContracts.Common;

namespace Api.Endpoints.Public;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalog(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/fibras").WithTags("Catalog");

        group.MapGet("/", async (
            IFibraRepository repo,
            CancellationToken ct,
            int? page = null,
            int? pageSize = null) =>
        {
            var p = page is null or < 1 ? 1 : page.Value;
            var ps = pageSize is null or < 1 or > 100 ? 20 : pageSize.Value;

            var (items, total) = await repo.GetActivePagedAsync(new FibraFilter(p, ps), ct);
            var dtos = items.Select(f => new FibraListItem(
                f.Id, f.Ticker, f.FullName, f.ShortName,
                f.Sector, f.Market, f.Currency, f.State.ToString(), f.SiteUrl)).ToList();

            return Results.Ok(new PagedResult<FibraListItem>(dtos, p, ps, total));
        })
        .AllowAnonymous()
        .Produces<PagedResult<FibraListItem>>(StatusCodes.Status200OK);

        group.MapGet("/{ticker}", async (
            string ticker,
            IFibraRepository repo,
            CancellationToken ct) =>
        {
            var fibra = await repo.GetByTickerAsync(ticker, ct);
            if (fibra is null)
                return Results.Problem(
                    title: "FIBRA no encontrada",
                    detail: $"No existe una FIBRA con ticker '{ticker}'.",
                    statusCode: StatusCodes.Status404NotFound,
                    extensions: new Dictionary<string, object?> { ["domainCode"] = "FIBRA_NOT_FOUND" });

            return Results.Ok(new FibraDetail(
                fibra.Id, fibra.Ticker, fibra.YahooTicker, fibra.FullName, fibra.ShortName,
                fibra.Sector, fibra.Market, fibra.Currency, fibra.State.ToString(),
                fibra.SiteUrl, fibra.InvestorUrl, fibra.ReportsUrl,
                fibra.NameVariants.AsReadOnly(), fibra.CreatedAt));
        })
        .AllowAnonymous()
        .Produces<FibraDetail>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
