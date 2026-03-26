namespace AuntiesCleaners.Client.Models;

public class WorkerRateResult
{
    public Guid? WorkerId { get; set; }
    public decimal RateCharged { get; set; }
    public decimal RatePaid { get; set; }
}
