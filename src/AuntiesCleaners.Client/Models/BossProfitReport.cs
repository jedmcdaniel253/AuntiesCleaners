namespace AuntiesCleaners.Client.Models;

public class BossProfitReport
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public List<ProfitCategoryBreakdown> Categories { get; set; } = new();
    public decimal GrandTotal => Categories.Sum(c => c.BossOwnTotal + c.WorkerMarginTotal);
}
