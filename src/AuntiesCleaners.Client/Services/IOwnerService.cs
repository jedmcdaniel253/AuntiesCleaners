using AuntiesCleaners.Client.Models;

namespace AuntiesCleaners.Client.Services;

public interface IOwnerService
{
    Task<Owner?> GetOwnerAsync();
    Task<List<Owner>> GetAllAsync();
    Task<Owner> UpdateAsync(Owner owner);
}
