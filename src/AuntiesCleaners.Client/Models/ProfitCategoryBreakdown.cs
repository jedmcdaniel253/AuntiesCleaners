namespace AuntiesCleaners.Client.Models;

public class ProfitCategoryBreakdown
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal BossOwnTotal { get; set; }
    public decimal WorkerMarginTotal { get; set; }
    public decimal CategoryTotal => BossOwnTotal + WorkerMarginTotal;
}
