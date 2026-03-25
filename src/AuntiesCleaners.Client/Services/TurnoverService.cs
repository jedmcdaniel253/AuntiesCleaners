using AuntiesCleaners.Client.Models;
using static Supabase.Postgrest.Constants;

namespace AuntiesCleaners.Client.Services;

public class TurnoverService : ITurnoverService
{
    private readonly ISupabaseClientService _supabase;
    private readonly IAuthService _auth;

    public TurnoverService(ISupabaseClientService supabase, IAuthService auth)
    {
        _supabase = supabase;
        _auth = auth;
    }

    public async Task<List<TurnoverEvent>> GetByDateAsync(DateTime date)
    {
        var response = await _supabase.Client
            .From<TurnoverEvent>()
            .Filter("event_date", Operator.Equals, date.Date.ToString("yyyy-MM-dd"))
            .Get();
        return response.Models;
    }

    public async Task<List<TurnoverEvent>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        var response = await _supabase.Client
            .From<TurnoverEvent>()
            .Filter("event_date", Operator.GreaterThanOrEqual, from.Date.ToString("yyyy-MM-dd"))
            .Filter("event_date", Operator.LessThanOrEqual, to.Date.ToString("yyyy-MM-dd"))
            .Get();
        return response.Models;
    }

    public async Task<List<TurnoverEvent>> GetByWeekAsync(DateTime weekStart)
    {
        var monday = weekStart.Date;
        var sunday = monday.AddDays(6);
        return await GetByDateRangeAsync(monday, sunday);
    }

    public async Task<TurnoverEvent> CreateAsync(TurnoverEvent turnoverEvent)
    {
        var profileId = await _auth.GetCurrentUserProfileIdAsync();
        if (profileId == null)
            throw new InvalidOperationException("User is not authenticated.");

        turnoverEvent.CreatedBy = profileId.Value;
        var response = await _supabase.Client.From<TurnoverEvent>().Insert(turnoverEvent);
        return response.Models.First();
    }

    public async Task<TurnoverEvent> UpdateAsync(TurnoverEvent turnoverEvent)
    {
        var response = await _supabase.Client.From<TurnoverEvent>().Update(turnoverEvent);
        return response.Models.First();
    }

    public async Task DeleteAsync(Guid eventId)
    {
        await _supabase.Client
            .From<TurnoverEvent>()
            .Filter("id", Operator.Equals, eventId.ToString())
            .Delete();
    }
}
