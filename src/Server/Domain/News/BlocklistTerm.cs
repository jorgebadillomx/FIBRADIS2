namespace Domain.News;

public class BlocklistTerm
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Term { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
