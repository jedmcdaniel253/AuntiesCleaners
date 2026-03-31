using AuntiesCleaners.Client.Models;

namespace AuntiesCleaners.Client.Services;

public interface IOwnerService
{
    Task<Owner?> GetOwnerAsync();
    Task<List<Owner>> GetAllAsync();
    Task<Owner?> GetByIdAsync(Guid id);
    Task<Owner?> GetBillingOwnerAsync();
    Task<Owner> CreateAsync(Owner owner);
    Task<Owner> UpdateAsync(Owner owner);
    Task DeleteAsync(Guid id);
    Task SetBillingOwnerAsync(Guid ownerId);
}
