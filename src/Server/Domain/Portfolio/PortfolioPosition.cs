namespace Domain.Portfolio;

public class PortfolioPosition
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid FibraId { get; set; }
    public int Titulos { get; set; }
    public decimal CostoPromedio { get; set; }
    public decimal CostoTotalCompra { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}
