using AuntiesCleaners.Client.Models;

namespace AuntiesCleaners.Client.Services;

public interface IMiscEntryService
{
    Task<List<MiscellaneousEntry>> GetByDateAsync(DateTime date);
    Task<List<MiscellaneousEntry>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task<MiscellaneousEntry> CreateAsync(MiscellaneousEntry entry);
    Task<MiscellaneousEntry> UpdateAsync(MiscellaneousEntry entry);
    Task DeleteAsync(Guid entryId);
}
