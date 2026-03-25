using AuntiesCleaners.Client.Models;

namespace AuntiesCleaners.Client.Services;

public interface IUserService
{
    Task<List<UserProfile>> GetAllAsync();
    Task<InviteResult> InviteUserAsync(string name, string email, string role, Guid? workerId);
    Task ResendInviteAsync(string email);
    Task<UserProfile> UpdateRoleAsync(Guid userProfileId, Role role);
    Task DeactivateAsync(Guid userProfileId);
    Task ReactivateAsync(Guid userProfileId);
}
