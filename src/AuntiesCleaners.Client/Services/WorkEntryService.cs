using AuntiesCleaners.Client.Models;
using static Supabase.Postgrest.Constants;

namespace AuntiesCleaners.Client.Services;

public class WorkEntryService : IWorkEntryService
{
    private readonly ISupabaseClientService _supabase;
    private readonly IAuthService _auth;

    public WorkEntryService(ISupabaseClientService supabase, IAuthService auth)
    {
        _supabase = supabase;
        _auth = auth;
    }

    public async Task<List<WorkEntry>> GetByDateAsync(DateTime date)
    {
        var response = await _supabase.Client
            .From<WorkEntry>()
            .Filter("entry_date", Operator.Equals, date.Date.ToString("yyyy-MM-dd"))
            .Get();
        return response.Models;
    }

    public async Task<List<WorkEntry>> GetByDateAndWorkerAsync(DateTime date, Guid workerId)
    {
        var response = await _supabase.Client
            .From<WorkEntry>()
            .Filter("entry_date", Operator.Equals, date.Date.ToString("yyyy-MM-dd"))
            .Filter("worker_id", Operator.Equals, workerId.ToString())
            .Get();
        return response.Models;
    }

    public async Task<List<WorkEntry>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        var response = await _supabase.Client
            .From<WorkEntry>()
            .Filter("entry_date", Operator.GreaterThanOrEqual, from.Date.ToString("yyyy-MM-dd"))
            .Filter("entry_date", Operator.LessThanOrEqual, to.Date.ToString("yyyy-MM-dd"))
            .Get();
        return response.Models;
    }

    public async Task<List<WorkEntry>> GetByDateRangeAndWorkerAsync(DateTime from, DateTime to, Guid workerId)
    {
        var response = await _supabase.Client
            .From<WorkEntry>()
            .Filter("entry_date", Operator.GreaterThanOrEqual, from.Date.ToString("yyyy-MM-dd"))
            .Filter("entry_date", Operator.LessThanOrEqual, to.Date.ToString("yyyy-MM-dd"))
            .Filter("worker_id", Operator.Equals, workerId.ToString())
            .Get();
        return response.Models;
    }

    public async Task<WorkEntry> CreateAsync(WorkEntry entry)
    {
        var profileId = await _auth.GetCurrentUserProfileIdAsync();
        if (profileId == null)
            throw new InvalidOperationException("User is not authenticated.");

        entry.CreatedBy = profileId.Value;
        var response = await _supabase.Client.From<WorkEntry>().Insert(entry);
        return response.Models.First();
    }

    public async Task<WorkEntry> UpdateAsync(WorkEntry entry)
    {
        var response = await _supabase.Client.From<WorkEntry>().Update(entry);
        return response.Models.First();
    }

    public async Task DeleteAsync(Guid entryId)
    {
        await _supabase.Client
            .From<WorkEntry>()
            .Filter("id", Operator.Equals, entryId.ToString())
            .Delete();
    }
}
