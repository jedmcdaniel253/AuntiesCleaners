using AuntiesCleaners.Client.Models;

namespace AuntiesCleaners.Client.Services;

public interface IAuthService
{
    Task InitializeAsync();
    Task<bool> LoginAsync(string email, string password);
    Task LogoutAsync();
    Task<UserProfile?> GetCurrentUserProfileAsync();
    Task<Role?> GetCurrentRoleAsync();
    Task<Guid?> GetCurrentUserProfileIdAsync();
    Task<Guid?> GetCurrentWorkerIdAsync();
    bool IsAuthenticated { get; }
    event Action? OnAuthStateChanged;
}
