namespace Domain.Seo;

public sealed class FaqItem
{
    public Guid Id { get; set; }
    public SeoPageType PageType { get; set; }
    public string EntityKey { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}
