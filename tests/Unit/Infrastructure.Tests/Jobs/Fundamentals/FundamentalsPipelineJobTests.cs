using System.Text.Json;
using Application.Fundamentals;
using Application.Jobs;
using Domain.Jobs;
using Infrastructure.Jobs.Fundamentals;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Jobs.Fundamentals;

public class FundamentalsPipelineJobTests
{
    [Fact]
    public async Task ExecuteForFibraAsync_WhenSuccessful_WritesCompletedRunLogWithFibraDetails()
    {
        var automation = new FakeFundamentalsAutomationService(
            result: new FundamentalsAutomationRunResult(1, 2, 1, 0, 0, 0, 0, 0, 1));
        var runLogRepo = new FakePipelineRunLogRepository();
        var job = new FundamentalsPipelineJob(automation, runLogRepo, NullLogger<FundamentalsPipelineJob>.Instance);

        await job.ExecuteForFibraAsync("FUNO11");

        Assert.Equal("FUNO11", automation.ReceivedTicker);
        Assert.Single(runLogRepo.Entries);
        var entry = runLogRepo.Entries[0];
        Assert.Equal("Fundamentals", entry.Pipeline);
        Assert.Equal("Completed", entry.Status);
        Assert.Equal(1, entry.ItemsProcessed);
        Assert.Equal(0, entry.ErrorCount);

        using var doc = JsonDocument.Parse(entry.Details!);
        Assert.Equal("FUNO11", doc.RootElement.GetProperty("fibra").GetString());
        Assert.Equal("single-fibra", doc.RootElement.GetProperty("mode").GetString());
    }

    [Fact]
    public async Task ExecuteForFibraAsync_WhenAutomationThrowsInvalidOperationException_WritesFailedRunLogWithErrorDetails()
    {
        var automation = new FakeFundamentalsAutomationService(
            exceptionToThrow: new InvalidOperationException("FIBRA 'INACTIVA1' no encontrada o no está activa."));
        var runLogRepo = new FakePipelineRunLogRepository();
        var job = new FundamentalsPipelineJob(automation, runLogRepo, NullLogger<FundamentalsPipelineJob>.Instance);

        await job.ExecuteForFibraAsync("INACTIVA1");

        Assert.Equal("INACTIVA1", automation.ReceivedTicker);
        Assert.Single(runLogRepo.Entries);
        var entry = runLogRepo.Entries[0];
        Assert.Equal("Failed", entry.Status);
        Assert.Equal(0, entry.ItemsProcessed);
        Assert.Equal(0, entry.ErrorCount);

        using var doc = JsonDocument.Parse(entry.Details!);
        Assert.Equal("INACTIVA1", doc.RootElement.GetProperty("fibra").GetString());
        Assert.Equal("single-fibra", doc.RootElement.GetProperty("mode").GetString());
        Assert.Contains("no encontrada o no está activa", doc.RootElement.GetProperty("error").GetString());
    }
}

internal sealed class FakeFundamentalsAutomationService(
    FundamentalsAutomationRunResult? result = null,
    Exception? exceptionToThrow = null) : IFundamentalsAutomationService
{
    public string? ReceivedTicker { get; private set; }

    public Task<FundamentalsAutomationRunResult> ExecuteAsync(CancellationToken ct)
        => Task.FromResult(result ?? new FundamentalsAutomationRunResult(0, 0, 0, 0, 0, 0, 0, 0, 0));

    public Task<FundamentalsAutomationRunResult> ExecuteAsync(string ticker, CancellationToken ct)
    {
        ReceivedTicker = ticker;
        if (exceptionToThrow is not null)
            throw exceptionToThrow;

        return Task.FromResult(result ?? new FundamentalsAutomationRunResult(0, 0, 0, 0, 0, 0, 0, 0, 0));
    }
}

internal sealed class FakePipelineRunLogRepository : IPipelineRunLogRepository
{
    public List<PipelineRunLog> Entries { get; } = [];

    public Task AddAsync(PipelineRunLog entry, CancellationToken ct = default)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PipelineRunLog>> GetRecentAsync(string? pipeline, int take, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<PipelineRunLog>>([]);

    public Task<PipelineRunLog?> GetLastCompletedAsync(string pipeline, CancellationToken ct = default)
        => Task.FromResult<PipelineRunLog?>(null);
}
