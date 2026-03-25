using AuntiesCleaners.Client.Models;

namespace AuntiesCleaners.Client.Services;

public interface IHouseService
{
    Task<List<House>> GetAllAsync();
    Task<List<House>> GetActiveAsync();
    Task<List<House>> GetActiveExcludingMultipleAsync();
    Task<House> CreateAsync(House house);
    Task<House> UpdateAsync(House house);
    Task DeactivateAsync(Guid houseId);
}
