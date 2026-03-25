using AuntiesCleaners.Client.Models;
using static Supabase.Postgrest.Constants;

namespace AuntiesCleaners.Client.Services;

public class HouseService : IHouseService
{
    private readonly ISupabaseClientService _supabase;

    public HouseService(ISupabaseClientService supabase) => _supabase = supabase;

    public async Task<List<House>> GetAllAsync()
    {
        var response = await _supabase.Client.From<House>().Get();
        return response.Models;
    }

    public async Task<List<House>> GetActiveAsync()
    {
        var response = await _supabase.Client
            .From<House>()
            .Filter("is_active", Operator.Equals, "true")
            .Get();
        return response.Models;
    }

    public async Task<List<House>> GetActiveExcludingMultipleAsync()
    {
        var response = await _supabase.Client
            .From<House>()
            .Filter("is_active", Operator.Equals, "true")
            .Filter("is_multiple_houses", Operator.Equals, "false")
            .Get();
        return response.Models;
    }

    public async Task<House> CreateAsync(House house)
    {
        house.IsMultipleHouses = false;
        var response = await _supabase.Client.From<House>().Insert(house);
        return response.Models.First();
    }

    public async Task<House> UpdateAsync(House house)
    {
        if (house.IsMultipleHouses)
            throw new InvalidOperationException("Cannot modify the Multiple Houses entry.");

        var response = await _supabase.Client.From<House>().Update(house);
        return response.Models.First();
    }

    public async Task DeactivateAsync(Guid houseId)
    {
        var response = await _supabase.Client
            .From<House>()
            .Filter("id", Operator.Equals, houseId.ToString())
            .Get();

        var house = response.Models.FirstOrDefault();
        if (house == null) return;
        if (house.IsMultipleHouses)
            throw new InvalidOperationException("Cannot deactivate the Multiple Houses entry.");

        house.IsActive = false;
        await _supabase.Client.From<House>().Update(house);
    }
}
