namespace Domain.Portfolio;

public class UserFavorite
{
    public Guid UserId { get; set; }
    public Guid FibraId { get; set; }
    public DateTimeOffset AddedAt { get; set; }
}
