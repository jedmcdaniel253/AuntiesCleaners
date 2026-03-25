using AuntiesCleaners.Client.Models;

namespace AuntiesCleaners.Client.Services;

public interface IBillingReportService
{
    Task<BillingReport> GenerateReportAsync(DateTime from, DateTime to);
}
