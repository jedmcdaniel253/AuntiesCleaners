using AuntiesCleaners.Client.Models;

namespace AuntiesCleaners.Client.Services;

public interface ITabConfigService
{
    Task<List<TabConfiguration>> GetTabsForUserAsync(Guid userProfileId);
    Task SaveTabsForUserAsync(Guid userProfileId, List<TabConfiguration> tabs);
}
