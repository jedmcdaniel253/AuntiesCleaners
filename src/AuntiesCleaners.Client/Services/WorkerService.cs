using AuntiesCleaners.Client.Models;
using static Supabase.Postgrest.Constants;

namespace AuntiesCleaners.Client.Services;

public class WorkerService : IWorkerService
{
    private readonly ISupabaseClientService _supabase;

    public WorkerService(ISupabaseClientService supabase) => _supabase = supabase;

    public async Task<List<Worker>> GetAllAsync()
    {
        var response = await _supabase.Client.From<Worker>().Get();
        return response.Models;
    }

    public async Task<List<Worker>> GetActiveAsync()
    {
        var response = await _supabase.Client
            .From<Worker>()
            .Filter("is_active", Operator.Equals, "true")
            .Get();
        return response.Models;
    }

    public async Task<Worker> CreateAsync(Worker worker)
    {
        var response = await _supabase.Client.From<Worker>().Insert(worker);
        return response.Models.First();
    }

    public async Task<Worker> UpdateAsync(Worker worker)
    {
        var response = await _supabase.Client.From<Worker>().Update(worker);
        return response.Models.First();
    }

    public async Task DeactivateAsync(Guid workerId)
    {
        var response = await _supabase.Client
            .From<Worker>()
            .Filter("id", Operator.Equals, workerId.ToString())
            .Get();

        var worker = response.Models.FirstOrDefault();
        if (worker != null)
        {
            worker.IsActive = false;
            await _supabase.Client.From<Worker>().Update(worker);
        }
    }
}
