using System.Net.Http.Json;
using AuntiesCleaners.Client.Models;
using Microsoft.Extensions.Configuration;
using static Supabase.Postgrest.Constants;

namespace AuntiesCleaners.Client.Services;

public class UserService : IUserService
{
    private readonly ISupabaseClientService _supabase;
    private readonly HttpClient _httpClient;
    private readonly string _supabaseUrl;
    private readonly string _supabaseAnonKey;

    public UserService(ISupabaseClientService supabase, HttpClient httpClient, IConfiguration configuration)
    {
        _supabase = supabase;
        _httpClient = httpClient;
        _supabaseUrl = configuration["Supabase:Url"] ?? throw new InvalidOperationException("Supabase:Url not configured");
        _supabaseAnonKey = configuration["Supabase:AnonKey"] ?? throw new InvalidOperationException("Supabase:AnonKey not configured");
    }

    public async Task<List<UserProfile>> GetAllAsync()
    {
        var response = await _supabase.Client.From<UserProfile>().Get();
        return response.Models;
    }

    public async Task<InviteResult> InviteUserAsync(string name, string email, string role, Guid? workerId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        var url = $"{_supabaseUrl}/functions/v1/invite-user";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Authorization", $"Bearer {_supabaseAnonKey}");
        request.Content = JsonContent.Create(new
        {
            name,
            email,
            role,
            workerId = workerId?.ToString()
        });

        var response = await _httpClient.SendAsync(request);
        var result = await response.Content.ReadFromJsonAsync<InviteResult>();
        return result ?? new InviteResult { Success = false, Error = "Unexpected response from server." };
    }

    public async Task ResendInviteAsync(string email)
    {
        var url = $"{_supabaseUrl}/functions/v1/invite-user";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Authorization", $"Bearer {_supabaseAnonKey}");
        request.Content = JsonContent.Create(new
        {
            action = "resend",
            email
        });

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<UserProfile> UpdateRoleAsync(Guid userProfileId, Role role)
    {
        var response = await _supabase.Client
            .From<UserProfile>()
            .Filter("id", Operator.Equals, userProfileId.ToString())
            .Get();

        var existing = response.Models.FirstOrDefault();
        if (existing == null) throw new InvalidOperationException("User profile not found.");

        existing.RoleValue = role.ToString();
        var updateResponse = await _supabase.Client.From<UserProfile>().Update(existing);
        return updateResponse.Models.First();
    }

    public async Task DeactivateAsync(Guid userProfileId)
    {
        var response = await _supabase.Client
            .From<UserProfile>()
            .Filter("id", Operator.Equals, userProfileId.ToString())
            .Get();

        var existing = response.Models.FirstOrDefault();
        if (existing == null) throw new InvalidOperationException("User profile not found.");

        existing.IsActive = false;
        await _supabase.Client.From<UserProfile>().Update(existing);
    }

    public async Task ReactivateAsync(Guid userProfileId)
    {
        var response = await _supabase.Client
            .From<UserProfile>()
            .Filter("id", Operator.Equals, userProfileId.ToString())
            .Get();

        var existing = response.Models.FirstOrDefault();
        if (existing == null) throw new InvalidOperationException("User profile not found.");

        existing.IsActive = true;
        await _supabase.Client.From<UserProfile>().Update(existing);
    }
}
