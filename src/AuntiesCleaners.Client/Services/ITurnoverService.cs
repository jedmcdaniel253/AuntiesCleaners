using AuntiesCleaners.Client.Models;

namespace AuntiesCleaners.Client.Services;

public interface ITurnoverService
{
    Task<List<TurnoverEvent>> GetByDateAsync(DateTime date);
    Task<List<TurnoverEvent>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task<List<TurnoverEvent>> GetByWeekAsync(DateTime weekStart);
    Task<TurnoverEvent> CreateAsync(TurnoverEvent turnoverEvent);
    Task<TurnoverEvent> UpdateAsync(TurnoverEvent turnoverEvent);
    Task DeleteAsync(Guid eventId);
}
