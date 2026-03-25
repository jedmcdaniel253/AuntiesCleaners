using AuntiesCleaners.Client.Models;

namespace AuntiesCleaners.Client.Services;

public interface IWorkEntryService
{
    Task<List<WorkEntry>> GetByDateAsync(DateTime date);
    Task<List<WorkEntry>> GetByDateAndWorkerAsync(DateTime date, Guid workerId);
    Task<List<WorkEntry>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task<List<WorkEntry>> GetByDateRangeAndWorkerAsync(DateTime from, DateTime to, Guid workerId);
    Task<WorkEntry> CreateAsync(WorkEntry entry);
    Task<WorkEntry> UpdateAsync(WorkEntry entry);
    Task DeleteAsync(Guid entryId);
}
