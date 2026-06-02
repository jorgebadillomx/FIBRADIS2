using Application.Catalog;
using Application.Fundamentals;
using Domain.Fundamentals;
using Microsoft.AspNetCore.Mvc;
using SharedApiContracts.Fundamentals;

namespace Api.Endpoints.Public;

public static class FundamentalsEndpoints
{
    public static IEndpointRouteBuilder MapFundamentalsPublic(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/fundamentals").WithTags("Catalog");

        group.MapGet("/summary", async (
            [FromQuery] string? period,
            [FromQuery] int? recent,
            IFundamentalRepository fundamentalRepo,
            CancellationToken ct) =>
        {
            IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)> rows =
                recent > 0
                    ? await fundamentalRepo.GetSummaryForRecentPeriodsAsync(recent.Value, ct)
                    : string.IsNullOrWhiteSpace(period)
                        ? await fundamentalRepo.GetSummaryLatestAsync(ct)
                        : await fundamentalRepo.GetSummaryByPeriodAsync(period.Trim().ToUpperInvariant(), ct);

            var dtos = rows.Select(r => new FundamentalesSummaryItemDto(
                Ticker: r.Ticker,
                Name: r.ShortName,
                Period: r.Record.Period,
                CapRate: r.Record.CapRate,
                NavPerCbfi: r.Record.NavPerCbfi,
                Ltv: r.Record.Ltv,
                NoiMargin: r.Record.NoiMargin,
                FfoMargin: r.Record.FfoMargin,
                QuarterlyDistribution: r.Record.QuarterlyDistribution,
                CapturedAt: r.Record.CapturedAt)).ToList();

            return Results.Ok(dtos);
        })
        .AllowAnonymous()
        .Produces<IReadOnlyList<FundamentalesSummaryItemDto>>(StatusCodes.Status200OK);

        group.MapGet("/periods", async (
            IFundamentalRepository fundamentalRepo,
            CancellationToken ct) =>
        {
            var periods = await fundamentalRepo.GetAllProcessedPeriodsAsync(ct);
            return Results.Ok(periods);
        })
        .AllowAnonymous()
        .Produces<IReadOnlyList<string>>(StatusCodes.Status200OK);

        group.MapGet("/{ticker}/latest", async (
            string ticker,
            [FromQuery] string? period,
            IFibraRepository fibraRepo,
            IFundamentalRepository fundamentalRepo,
            CancellationToken ct) =>
        {
            var fibra = await fibraRepo.GetByTickerAsync(ticker, ct);
            if (fibra is null)
                return Results.Problem(
                    title: "FIBRA no encontrada",
                    detail: $"No existe una FIBRA con ticker '{ticker}'.",
                    statusCode: StatusCodes.Status404NotFound,
                    extensions: new Dictionary<string, object?> { ["domainCode"] = "FIBRA_NOT_FOUND" });

            FundamentalRecord? record = string.IsNullOrWhiteSpace(period)
                ? await fundamentalRepo.GetLatestProcessedByFibraAsync(fibra.Id, ct)
                : await fundamentalRepo.GetProcessedByFibraAndPeriodAsync(fibra.Id, period.Trim().ToUpperInvariant(), ct);

            if (record is null)
                return Results.NotFound();

            var aiAnalysis = record.GetAiAnalysis();
            return Results.Ok(new FundamentalesPublicDto(
                Period: record.Period,
                PeriodsAgo: null,
                CapRate: record.CapRate,
                NavPerCbfi: record.NavPerCbfi,
                Ltv: record.Ltv,
                NoiMargin: record.NoiMargin,
                FfoMargin: record.FfoMargin,
                QuarterlyDistribution: record.QuarterlyDistribution,
                Summary: record.Summary,
                SummaryMarkdown: aiAnalysis?.SummaryMarkdown,
                InvestorTakeaway: aiAnalysis?.InvestorTakeaway,
                OperationalSignals: aiAnalysis?.OperationalSignals.ToArray() ?? [],
                FinancialSignals: aiAnalysis?.FinancialSignals.ToArray() ?? [],
                RiskFlags: aiAnalysis?.RiskFlags.ToArray() ?? [],
                FieldNotes: record.GetFieldNotes(),
                CapturedAt: record.CapturedAt));
        })
        .AllowAnonymous()
        .Produces<FundamentalesPublicDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{ticker}/periods", async (
            string ticker,
            IFibraRepository fibraRepo,
            IFundamentalRepository fundamentalRepo,
            CancellationToken ct) =>
        {
            var fibra = await fibraRepo.GetByTickerAsync(ticker, ct);
            if (fibra is null)
                return Results.Problem(
                    title: "FIBRA no encontrada",
                    detail: $"No existe una FIBRA con ticker '{ticker}'.",
                    statusCode: StatusCodes.Status404NotFound,
                    extensions: new Dictionary<string, object?> { ["domainCode"] = "FIBRA_NOT_FOUND" });

            var periods = await fundamentalRepo.GetProcessedPeriodsAsync(fibra.Id, ct);
            return Results.Ok(periods);
        })
        .AllowAnonymous()
        .Produces<IReadOnlyList<string>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}
