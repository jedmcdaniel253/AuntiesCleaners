using AuntiesCleaners.Client.Models;
using Microsoft.JSInterop;

namespace AuntiesCleaners.Client.Services;

public class AuthService : IAuthService
{
    private readonly ISupabaseClientService _supabase;
    private readonly IJSRuntime _js;
    private UserProfile? _cachedProfile;
    private const string SessionKey = "supabase.auth.session";

    public bool IsAuthenticated => _supabase.Client.Auth.CurrentUser != null;
    public event Action? OnAuthStateChanged;

    public AuthService(ISupabaseClientService supabase, IJSRuntime js)
    {
        _supabase = supabase;
        _js = js;
    }

    public async Task InitializeAsync()
    {
        await _supabase.InitializeAsync();

        // If no current user after init, try restoring session from localStorage
        if (_supabase.Client.Auth.CurrentUser == null)
        {
            try
            {
                var refreshToken = await _js.InvokeAsync<string?>("sessionInterop.load", SessionKey);
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    // Use SignIn with refresh token to restore the session
                    var session = await _supabase.Client.Auth.SignIn(Supabase.Gotrue.Constants.SignInType.RefreshToken, refreshToken);
                    if (session?.User != null)
                    {
                        // Save the new refresh token
                        await _js.InvokeVoidAsync("sessionInterop.save", SessionKey, session.RefreshToken);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AuthService] Session restore failed: {ex.Message}");
                // Clear invalid session data
                await _js.InvokeVoidAsync("sessionInterop.destroy", SessionKey);
            }
        }

        OnAuthStateChanged?.Invoke();
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            var session = await _supabase.Client.Auth.SignIn(email, password);
            if (session?.User != null)
            {
                _cachedProfile = null;
                // Persist refresh token to localStorage
                if (!string.IsNullOrEmpty(session.RefreshToken))
                {
                    await _js.InvokeVoidAsync("sessionInterop.save", SessionKey, session.RefreshToken);
                }
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
        await _js.InvokeVoidAsync("sessionInterop.destroy", SessionKey);
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
