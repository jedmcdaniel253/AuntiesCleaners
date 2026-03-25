using AuntiesCleaners.Client.Models;
using static Supabase.Postgrest.Constants;

namespace AuntiesCleaners.Client.Services;

public class MowListService : IMowListService
{
    private readonly ISupabaseClientService _supabase;

    public MowListService(ISupabaseClientService supabase) => _supabase = supabase;

    public async Task<List<MowListItem>> GetAllAsync()
    {
        var response = await _supabase.Client.From<MowListItem>().Get();
        return response.Models;
    }

    public async Task ToggleMowStatusAsync(Guid houseId, bool needsMowing)
    {
        var response = await _supabase.Client
            .From<MowListItem>()
            .Filter("house_id", Operator.Equals, houseId.ToString())
            .Get();

        var item = response.Models.FirstOrDefault();
        if (item != null)
        {
            item.NeedsMowing = needsMowing;
            item.UpdatedAt = DateTime.UtcNow;
            await _supabase.Client.From<MowListItem>().Update(item);
        }
        else
        {
            var newItem = new MowListItem
            {
                Id = Guid.NewGuid(),
                HouseId = houseId,
                NeedsMowing = needsMowing,
                UpdatedAt = DateTime.UtcNow
            };
            await _supabase.Client.From<MowListItem>().Insert(newItem);
        }
    }

    public async Task ClearMowStatusForHouseAsync(Guid houseId)
    {
        await ToggleMowStatusAsync(houseId, false);
    }
}
