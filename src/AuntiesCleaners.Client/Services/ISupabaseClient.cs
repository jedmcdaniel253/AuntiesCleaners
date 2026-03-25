using Supabase;

namespace AuntiesCleaners.Client.Services;

public interface ISupabaseClientService
{
    Supabase.Client Client { get; }
    Task InitializeAsync();
}
