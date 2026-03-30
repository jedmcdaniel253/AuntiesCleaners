using AuntiesCleaners.Client.Models;

namespace AuntiesCleaners.Client.Services;

public interface IProfitReportService
{
    Task<BossProfitReport> GenerateReportAsync(DateTime from, DateTime to);
}
