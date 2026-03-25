using AuntiesCleaners.Client.Models;

namespace AuntiesCleaners.Client.Services;

public interface IRateService
{
    // Rate methods (Cleaning, Maintenance, Laundry)
    Task<List<Rate>> GetByCategoryAsync(string category);
    Task<Rate?> GetByCategoryAndWorkerAsync(string category, Guid workerId);
    Task<Rate> CreateOrUpdateAsync(Rate rate);

    // LawnHouseRate methods
    Task<List<LawnHouseRate>> GetAllLawnRatesAsync();
    Task<List<LawnHouseRate>> GetByHouseAsync(Guid houseId);
    Task<LawnHouseRate?> GetByHouseAndWorkerAsync(Guid houseId, Guid workerId);
    Task<LawnHouseRate> CreateOrUpdateLawnRateAsync(LawnHouseRate rate);
}
