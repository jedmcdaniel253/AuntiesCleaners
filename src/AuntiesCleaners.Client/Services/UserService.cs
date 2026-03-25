using AuntiesCleaners.Client.Models;
using static Supabase.Postgrest.Constants;

namespace AuntiesCleaners.Client.Services;

public class UserService : IUserService
{
    private readonly ISupabaseClientService _supabase;

    public UserService(ISupabaseClientService supabase)
    {
        _supabase = supabase;
    }

    public async Task<List<UserProfile>> GetAllAsync()
    {
        var response = await _supabase.Client.From<UserProfile>().Get();
        return response.Models;
    }

    public async Task<UserProfile> CreateAsync(UserProfile userProfile, string email)
    {
        userProfile.Email = email;
        var response = await _supabase.Client.From<UserProfile>().Insert(userProfile);
        return response.Models.First();
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

    public async Task ResendInviteAsync(string email)
    {
        await _supabase.Client.Auth.ResetPasswordForEmail(email);
    }
}
