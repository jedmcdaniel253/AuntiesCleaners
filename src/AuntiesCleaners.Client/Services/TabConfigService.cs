using AuntiesCleaners.Client.Models;
using static Supabase.Postgrest.Constants;

namespace AuntiesCleaners.Client.Services;

public class TabConfigService : ITabConfigService
{
    private readonly ISupabaseClientService _supabase;

    public TabConfigService(ISupabaseClientService supabase)
    {
        _supabase = supabase;
    }

    public async Task<List<TabConfiguration>> GetTabsForUserAsync(Guid userProfileId)
    {
        var response = await _supabase.Client
            .From<TabConfiguration>()
            .Filter("user_profile_id", Operator.Equals, userProfileId.ToString())
            .Order("display_order", Ordering.Ascending)
            .Get();

        return response.Models;
    }

    public async Task SaveTabsForUserAsync(Guid userProfileId, List<TabConfiguration> tabs)
    {
        await _supabase.Client
            .From<TabConfiguration>()
            .Filter("user_profile_id", Operator.Equals, userProfileId.ToString())
            .Delete();

        foreach (var tab in tabs)
        {
            tab.UserProfileId = userProfileId;
            await _supabase.Client
                .From<TabConfiguration>()
                .Insert(tab);
        }
    }
}
