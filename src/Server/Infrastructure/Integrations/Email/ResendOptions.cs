namespace Infrastructure.Integrations.Email;

public sealed record ResendOptions(string ApiKey, string SenderEmail)
{
    public ResendTemplateIds Templates { get; init; } = new();
}

public sealed record ResendTemplateIds
{
    public string EmailConfirmation { get; init; } = "";
    public string PaymentNotification { get; init; } = "";
    public string AccessExpired { get; init; } = "";
    public string AccessActivated { get; init; } = "";
    public string TrialExpiring { get; init; } = "";
    public string SubscriptionExpiring { get; init; } = "";
}
