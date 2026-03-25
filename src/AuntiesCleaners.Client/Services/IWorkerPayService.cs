using AuntiesCleaners.Client.Models;

namespace AuntiesCleaners.Client.Services;

public interface IWorkerPayService
{
    Task<WorkerPayReport> GeneratePayReportAsync(DateTime from, DateTime to);
}
