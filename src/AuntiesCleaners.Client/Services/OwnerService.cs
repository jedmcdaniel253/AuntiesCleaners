using AuntiesCleaners.Client.Models;
using static Supabase.Postgrest.Constants;

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
            .Order("name", Ordering.Ascending)
            .Get();
        return response.Models;
    }

    public async Task<Owner?> GetByIdAsync(Guid id)
    {
        var response = await _supabase.Client.From<Owner>()
            .Filter("id", Operator.Equals, id.ToString())
            .Get();
        return response.Models.FirstOrDefault();
    }

    public async Task<Owner?> GetBillingOwnerAsync()
    {
        var response = await _supabase.Client.From<Owner>()
            .Filter("is_billing_owner", Operator.Equals, "true")
            .Get();
        return response.Models.FirstOrDefault();
    }

    public async Task<Owner> CreateAsync(Owner owner)
    {
        owner.IsBillingOwner = false;
        var response = await _supabase.Client.From<Owner>().Insert(owner);
        return response.Models.First();
    }

    public async Task<Owner> UpdateAsync(Owner owner)
    {
        var response = await _supabase.Client.From<Owner>().Update(owner);
        return response.Models.First();
    }

    public async Task DeleteAsync(Guid id)
    {
        await _supabase.Client.From<Owner>()
            .Filter("id", Operator.Equals, id.ToString())
            .Delete();
    }

    public async Task SetBillingOwnerAsync(Guid ownerId)
    {
        // Step 1: Clear billing owner flag on all owners
        var allResponse = await _supabase.Client.From<Owner>().Get();
        foreach (var owner in allResponse.Models)
        {
            if (owner.IsBillingOwner)
            {
                owner.IsBillingOwner = false;
                await _supabase.Client.From<Owner>().Update(owner);
            }
        }

        // Step 2: Set billing owner flag on the target owner
        var targetResponse = await _supabase.Client.From<Owner>()
            .Filter("id", Operator.Equals, ownerId.ToString())
            .Get();
        var target = targetResponse.Models.FirstOrDefault();
        if (target != null)
        {
            target.IsBillingOwner = true;
            await _supabase.Client.From<Owner>().Update(target);
        }
    }
}
