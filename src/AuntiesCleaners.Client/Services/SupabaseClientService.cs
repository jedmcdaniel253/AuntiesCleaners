using Microsoft.Extensions.Configuration;
using Supabase;

namespace AuntiesCleaners.Client.Services;

public class SupabaseClientService : ISupabaseClientService
{
    private readonly Supabase.Client _client;

    public Supabase.Client Client => _client;

    public SupabaseClientService(IConfiguration configuration)
    {
        var url = configuration["Supabase:Url"] ?? throw new InvalidOperationException("Supabase:Url not configured");
        var key = configuration["Supabase:AnonKey"] ?? throw new InvalidOperationException("Supabase:AnonKey not configured");

        _client = new Supabase.Client(url, key, new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = false
        });
    }

    public async Task InitializeAsync()
    {
        await _client.InitializeAsync();
    }
}
