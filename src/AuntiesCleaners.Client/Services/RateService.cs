using AuntiesCleaners.Client.Models;
using static Supabase.Postgrest.Constants;

namespace AuntiesCleaners.Client.Services;

public class RateService : IRateService
{
    private readonly ISupabaseClientService _supabase;

    public RateService(ISupabaseClientService supabase)
    {
        _supabase = supabase;
    }

    public async Task<List<Rate>> GetByCategoryAsync(string category)
    {
        var response = await _supabase.Client
            .From<Rate>()
            .Filter("work_category", Operator.Equals, category)
            .Get();
        return response.Models;
    }

    public async Task<Rate?> GetByCategoryAndWorkerAsync(string category, Guid workerId)
    {
        var response = await _supabase.Client
            .From<Rate>()
            .Filter("work_category", Operator.Equals, category)
            .Filter("worker_id", Operator.Equals, workerId.ToString())
            .Get();
        return response.Models.FirstOrDefault();
    }

    public async Task<Rate> CreateOrUpdateAsync(Rate rate)
    {
        ValidateRate(rate.RateCharged, rate.RatePaid);
        rate.UpdatedAt = DateTime.UtcNow;

        if (rate.Id == Guid.Empty)
        {
            rate.Id = Guid.NewGuid();
            var response = await _supabase.Client.From<Rate>().Insert(rate);
            return response.Models.First();
        }
        else
        {
            var response = await _supabase.Client.From<Rate>().Update(rate);
            return response.Models.First();
        }
    }

    public async Task<List<LawnHouseRate>> GetAllLawnRatesAsync()
    {
        var response = await _supabase.Client.From<LawnHouseRate>().Get();
        return response.Models;
    }

    public async Task<List<LawnHouseRate>> GetByHouseAsync(Guid houseId)
    {
        var response = await _supabase.Client
            .From<LawnHouseRate>()
            .Filter("house_id", Operator.Equals, houseId.ToString())
            .Get();
        return response.Models;
    }

    public async Task<LawnHouseRate?> GetByHouseAndWorkerAsync(Guid houseId, Guid workerId)
    {
        var response = await _supabase.Client
            .From<LawnHouseRate>()
            .Filter("house_id", Operator.Equals, houseId.ToString())
            .Filter("worker_id", Operator.Equals, workerId.ToString())
            .Get();
        return response.Models.FirstOrDefault();
    }

    public async Task<LawnHouseRate> CreateOrUpdateLawnRateAsync(LawnHouseRate rate)
    {
        ValidateRate(rate.RateCharged, rate.RatePaid);
        rate.UpdatedAt = DateTime.UtcNow;

        if (rate.Id == Guid.Empty)
        {
            rate.Id = Guid.NewGuid();
            var response = await _supabase.Client.From<LawnHouseRate>().Insert(rate);
            return response.Models.First();
        }
        else
        {
            var response = await _supabase.Client.From<LawnHouseRate>().Update(rate);
            return response.Models.First();
        }
    }

    public static void ValidateRate(decimal rateCharged, decimal ratePaid)
    {
        if (rateCharged <= 0)
            throw new ArgumentException("Rate charged must be greater than zero.", nameof(rateCharged));
        if (ratePaid < 0)
            throw new ArgumentException("Rate paid must be zero or positive.", nameof(ratePaid));
    }
}
