using AuntiesCleaners.Client.Models;

namespace AuntiesCleaners.Client.Services;

public class AuthService : IAuthService
{
    private readonly ISupabaseClientService _supabase;
    private UserProfile? _cachedProfile;

    public bool IsAuthenticated => _supabase.Client.Auth.CurrentUser != null;
    public event Action? OnAuthStateChanged;

    public AuthService(ISupabaseClientService supabase)
    {
        _supabase = supabase;
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            var session = await _supabase.Client.Auth.SignIn(email, password);
            if (session?.User != null)
            {
                _cachedProfile = null;
                OnAuthStateChanged?.Invoke();
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        await _supabase.Client.Auth.SignOut();
        _cachedProfile = null;
        OnAuthStateChanged?.Invoke();
    }

    public async Task<UserProfile?> GetCurrentUserProfileAsync()
    {
        if (_cachedProfile != null) return _cachedProfile;

        var user = _supabase.Client.Auth.CurrentUser;
        if (user == null) return null;

        try
        {
            var response = await _supabase.Client
                .From<UserProfile>()
                .Filter("auth_user_id", Supabase.Postgrest.Constants.Operator.Equals, user.Id!)
                .Get();

            _cachedProfile = response.Models.FirstOrDefault();
            return _cachedProfile;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthService] Failed to load user profile: {ex.Message}");
            return null;
        }
    }

    public async Task<Role?> GetCurrentRoleAsync()
    {
        var profile = await GetCurrentUserProfileAsync();
        if (profile == null) return null;
        return Enum.TryParse<Role>(profile.RoleValue, out var role) ? role : null;
    }

    public async Task<Guid?> GetCurrentUserProfileIdAsync()
    {
        var profile = await GetCurrentUserProfileAsync();
        return profile?.Id;
    }

    public async Task<Guid?> GetCurrentWorkerIdAsync()
    {
        var profile = await GetCurrentUserProfileAsync();
        return profile?.WorkerId;
    }
}
