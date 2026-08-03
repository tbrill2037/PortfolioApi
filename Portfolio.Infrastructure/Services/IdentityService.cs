using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Common.Exceptions;
using Portfolio.Application.Common.Interfaces;
using Portfolio.Infra.Identity;

namespace Portfolio.Infra.Services;

/// <summary>
/// Provides user and role management operations using ASP.NET Identity.
/// </summary>
public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public IdentityService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<bool> AssignUserToRoleAsync(string userName, IList<string> roles)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == userName);

        if (user == null)
            throw new NotFoundException("User not found");

        var result = await _userManager.AddToRolesAsync(user, roles);

        return result.Succeeded;
    }

    public async Task<bool> CreateRoleAsync(string roleName)
    {
        var result = await _roleManager.CreateAsync(new IdentityRole(roleName));

        if (!result.Succeeded)
            throw new ValidationException(result.Errors);

        return result.Succeeded;
    }

    public async Task<(bool isSucceed, string userId)> CreateUserAsync(
        string userName,
        string password,
        string email,
        string firstName,
        string lastName,
        string fullName,
        List<string> roles)
    {
        var user = new ApplicationUser
        {
            FirstName = firstName,
            LastName = lastName,
            FullName = fullName,
            UserName = userName,
            Email = email,
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
            throw new ValidationException(result.Errors);

        var addUserRole = await _userManager.AddToRolesAsync(user, roles);

        if (!addUserRole.Succeeded)
            throw new ValidationException(addUserRole.Errors);

        return (result.Succeeded, user.Id);
    }

    public async Task<bool> DeleteRoleAsync(string roleId)
    {
        var roleDetails = await _roleManager.FindByIdAsync(roleId);

        if (roleDetails == null)
            throw new NotFoundException("Role not found");

        if (roleDetails.Name == "Administrator")
            throw new BadRequestException("You can not delete Administrator Role");

        var result = await _roleManager.DeleteAsync(roleDetails);

        if (!result.Succeeded)
            throw new ValidationException(result.Errors);

        return result.Succeeded;
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null)
            throw new NotFoundException("User not found");

        if (user.UserName == "system" || user.UserName == "admin")
            throw new Exception("You can not delete system or admin user");

        var result = await _userManager.DeleteAsync(user);

        return result.Succeeded;
    }

    public async Task<List<(string id, string fullName, string userName, string email)>> GetAllUsersAsync()
    {
        var users = await _userManager.Users.Select(x => new
        {
            x.Id,
            x.FullName,
            x.UserName,
            x.Email
        })
        .ToListAsync();

        return users.Select(user => (
            user.Id,
            user.FullName ?? string.Empty,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty)).ToList();
    }

    public Task<List<(string id, string userName, string email, IList<string> roles)>> GetAllUsersDetailsAsync()
    {
        throw new NotImplementedException();

        //var roles = await _userManager.GetRolesAsync(user);
        //return (user.Id, user.UserName, user.Email, roles);

        //var users = _userManager.Users.ToListAsync();
    }

    public async Task<List<(string id, string roleName)>> GetRolesAsync()
    {
        var roles = await _roleManager.Roles.Select(x => new
        {
            x.Id,
            x.Name
        })
        .ToListAsync();

        return roles.Select(role => (role.Id, role.Name ?? string.Empty)).ToList();
    }

    public async Task<(string userId, string fullName, string userName, string email, IList<string> roles)> GetUserDetailsAsync(string userId)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null)
            throw new NotFoundException("User not found");

        var roles = await _userManager.GetRolesAsync(user);

        return (
            user.Id,
            user.FullName ?? string.Empty,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            roles);
    }

    public async Task<(string userId, string fullName, string userName, string email, IList<string> roles)> GetUserDetailsByUserNameAsync(string userName)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == userName);

        if (user == null)
            throw new NotFoundException("User not found");

        var roles = await _userManager.GetRolesAsync(user);

        return (
            user.Id,
            user.FullName ?? string.Empty,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            roles);
    }

    public async Task<string> GetUserIdAsync(string userName)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == userName);

        if (user == null)
            throw new NotFoundException("User not found");

        return await _userManager.GetUserIdAsync(user);
    }

    public async Task<string> GetUserNameAsync(string userId)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null)
            throw new NotFoundException("User not found");

        var userName = await _userManager.GetUserNameAsync(user);

        if (userName == null)
            throw new NotFoundException("Username not found.");

        return userName;
    }

    public async Task<List<string>> GetUserRolesAsync(string userId)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null)
            throw new NotFoundException("User not found");

        var roles = await _userManager.GetRolesAsync(user);

        return roles.ToList();
    }

    public async Task<bool> IsInRoleAsync(string userId, string role)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);

        if (user == null)
            throw new NotFoundException("User not found");

        return await _userManager.IsInRoleAsync(user, role);
    }

    public async Task<bool> IsUniqueUserNameAsync(string userName)
    {
        return await _userManager.FindByNameAsync(userName) == null;
    }

    public async Task<bool> SigninUserAsync(string userName, string password)
    {
        var user = await _userManager.FindByNameAsync(userName);

        if (user == null)
            return false;

        var valid = await _userManager.CheckPasswordAsync(user, password);

        return valid;
    }

    public async Task<bool> UpdateUserProfileAsync(string id, string fullName, string email, IList<string> roles)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
            throw new NotFoundException("User not found");

        user.FullName = fullName;
        user.Email = email;

        var result = await _userManager.UpdateAsync(user);

        return result.Succeeded;
    }

    public async Task<(string id, string roleName)> GetRoleByIdAsync(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);

        if (role == null)
            throw new NotFoundException("Role not found");

        return (role.Id, role.Name ?? string.Empty);
    }

    public async Task<bool> UpdateRoleAsync(string id, string roleName)
    {
        if (roleName != null)
        {
            var role = await _roleManager.FindByIdAsync(id);

            if (role == null)
                throw new NotFoundException("Role not found");

            role.Name = roleName;

            var result = await _roleManager.UpdateAsync(role);

            return result.Succeeded;
        }

        return false;
    }

    public async Task<bool> UpdateUserRolesAsync(string userName, IList<string> usersRole)
    {
        var user = await _userManager.FindByNameAsync(userName);

        if (user == null)
            throw new NotFoundException("User not found");

        var existingRoles = await _userManager.GetRolesAsync(user);
        var result = await _userManager.RemoveFromRolesAsync(user, existingRoles);

        result = await _userManager.AddToRolesAsync(user, usersRole);

        return result.Succeeded;
    }
}