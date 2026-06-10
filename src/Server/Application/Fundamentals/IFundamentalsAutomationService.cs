namespace Application.Fundamentals;

public interface IFundamentalsAutomationService
{
    Task<FundamentalsAutomationRunResult> ExecuteAsync(CancellationToken ct);

    Task<FundamentalsAutomationRunResult> ExecuteAsync(string ticker, CancellationToken ct);
}
