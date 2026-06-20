namespace Infrastructure.Integrations.Email;

public sealed class ResendOptions
{
    public string ApiKey { get; set; } = "";
    public string SenderEmail { get; set; } = "";
    public ResendTemplateIds Templates { get; set; } = new();
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
