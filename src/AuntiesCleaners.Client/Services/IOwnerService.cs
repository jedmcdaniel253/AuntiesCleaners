using AuntiesCleaners.Client.Models;

namespace AuntiesCleaners.Client.Services;

public interface IOwnerService
{
    Task<Owner?> GetOwnerAsync();
    Task<Owner> UpdateAsync(Owner owner);
}
