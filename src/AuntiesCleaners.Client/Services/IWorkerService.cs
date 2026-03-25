using AuntiesCleaners.Client.Models;

namespace AuntiesCleaners.Client.Services;

public interface IWorkerService
{
    Task<List<Worker>> GetAllAsync();
    Task<List<Worker>> GetActiveAsync();
    Task<Worker> CreateAsync(Worker worker);
    Task<Worker> UpdateAsync(Worker worker);
    Task DeactivateAsync(Guid workerId);
}
