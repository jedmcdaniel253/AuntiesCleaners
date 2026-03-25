using AuntiesCleaners.Client.Models;
using static Supabase.Postgrest.Constants;

namespace AuntiesCleaners.Client.Services;

public class MiscEntryService : IMiscEntryService
{
    private readonly ISupabaseClientService _supabase;
    private readonly IAuthService _auth;

    public MiscEntryService(ISupabaseClientService supabase, IAuthService auth)
    {
        _supabase = supabase;
        _auth = auth;
    }

    public async Task<List<MiscellaneousEntry>> GetByDateAsync(DateTime date)
    {
        var response = await _supabase.Client
            .From<MiscellaneousEntry>()
            .Filter("entry_date", Operator.Equals, date.Date.ToString("yyyy-MM-dd"))
            .Get();
        return response.Models;
    }

    public async Task<List<MiscellaneousEntry>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        var response = await _supabase.Client
            .From<MiscellaneousEntry>()
            .Filter("entry_date", Operator.GreaterThanOrEqual, from.Date.ToString("yyyy-MM-dd"))
            .Filter("entry_date", Operator.LessThanOrEqual, to.Date.ToString("yyyy-MM-dd"))
            .Get();
        return response.Models;
    }

    public async Task<MiscellaneousEntry> CreateAsync(MiscellaneousEntry entry)
    {
        var profileId = await _auth.GetCurrentUserProfileIdAsync();
        if (profileId == null)
            throw new InvalidOperationException("User is not authenticated.");

        entry.CreatedBy = profileId.Value;
        var response = await _supabase.Client.From<MiscellaneousEntry>().Insert(entry);
        return response.Models.First();
    }

    public async Task<MiscellaneousEntry> UpdateAsync(MiscellaneousEntry entry)
    {
        var response = await _supabase.Client.From<MiscellaneousEntry>().Update(entry);
        return response.Models.First();
    }

    public async Task DeleteAsync(Guid entryId)
    {
        await _supabase.Client
            .From<MiscellaneousEntry>()
            .Filter("id", Operator.Equals, entryId.ToString())
            .Delete();
    }
}
