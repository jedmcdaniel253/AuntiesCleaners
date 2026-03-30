using AuntiesCleaners.Client.Models;

namespace AuntiesCleaners.Client.Services;

public class OwnerService : IOwnerService
{
    private readonly ISupabaseClientService _supabase;

    public OwnerService(ISupabaseClientService supabase)
    {
        _supabase = supabase;
    }

    public async Task<Owner?> GetOwnerAsync()
    {
        var response = await _supabase.Client.From<Owner>().Get();
        return response.Models.FirstOrDefault();
    }

    public async Task<List<Owner>> GetAllAsync()
    {
        var response = await _supabase.Client.From<Owner>()
            .Order("name", Supabase.Postgrest.Constants.Ordering.Ascending)
            .Get();
        return response.Models;
    }

    public async Task<Owner> UpdateAsync(Owner owner)
    {
        var response = await _supabase.Client.From<Owner>().Update(owner);
        return response.Models.First();
    }
}
