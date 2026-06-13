namespace Domain.Ops;

public class InpcMonthlyEntry
{
    public DateOnly Periodo { get; set; }
    public decimal InpcIndex { get; set; }
    public DateTimeOffset CapturedAt { get; set; }
}
