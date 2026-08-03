namespace Portfolio.Application.Common.Interfaces;

/// <summary>
/// Defines identity management operations for users, roles, and user-role assignments.
/// </summary>
public interface IIdentityService
{
    // User operations
    Task<(bool isSucceed, string userId)> CreateUserAsync(
        string userName,
        string password,
        string email,
        string firstName,
        string lastName,
        string fullName,
        List<string> roles);

    Task<bool> SigninUserAsync(string userName, string password);

    Task<string> GetUserIdAsync(string userName);

    Task<(string userId, string fullName, string userName, string email, IList<string> roles)> GetUserDetailsAsync(string userId);

    Task<(string userId, string fullName, string userName, string email, IList<string> roles)> GetUserDetailsByUserNameAsync(string userName);

    Task<string> GetUserNameAsync(string userId);

    Task<bool> DeleteUserAsync(string userId);

    Task<bool> IsUniqueUserNameAsync(string userName);

    Task<List<(string id, string fullName, string userName, string email)>> GetAllUsersAsync();

    Task<List<(string id, string userName, string email, IList<string> roles)>> GetAllUsersDetailsAsync();

    Task<bool> UpdateUserProfileAsync(
        string id,
        string fullName,
        string email,
        IList<string> roles);

    // Role operations
    Task<bool> CreateRoleAsync(string roleName);

    Task<bool> DeleteRoleAsync(string roleId);

    Task<List<(string id, string roleName)>> GetRolesAsync();

    Task<(string id, string roleName)> GetRoleByIdAsync(string id);

    Task<bool> UpdateRoleAsync(string id, string roleName);

    // User role operations
    Task<bool> IsInRoleAsync(string userId, string role);

    Task<List<string>> GetUserRolesAsync(string userId);

    Task<bool> AssignUserToRoleAsync(
        string userName,
        IList<string> roles);

    Task<bool> UpdateUserRolesAsync(
        string userName,
        IList<string> roles);
}