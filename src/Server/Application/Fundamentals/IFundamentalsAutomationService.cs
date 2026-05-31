namespace Application.Fundamentals;

public interface IFundamentalsAutomationService
{
    Task<FundamentalsAutomationRunResult> ExecuteAsync(CancellationToken ct);
}
