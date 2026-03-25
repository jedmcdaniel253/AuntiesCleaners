using AuntiesCleaners.Client.Models;

namespace AuntiesCleaners.Client.Services;

public interface IMowListService
{
    Task<List<MowListItem>> GetAllAsync();
    Task ToggleMowStatusAsync(Guid houseId, bool needsMowing);
    Task ClearMowStatusForHouseAsync(Guid houseId);
}
