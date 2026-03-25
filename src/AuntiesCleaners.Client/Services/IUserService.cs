using AuntiesCleaners.Client.Models;

namespace AuntiesCleaners.Client.Services;

public interface IUserService
{
    Task<List<UserProfile>> GetAllAsync();
    Task<UserProfile> CreateAsync(UserProfile userProfile, string email);
    Task<UserProfile> UpdateRoleAsync(Guid userProfileId, Role role);
    Task DeactivateAsync(Guid userProfileId);
    Task ReactivateAsync(Guid userProfileId);
    Task ResendInviteAsync(string email);
}
