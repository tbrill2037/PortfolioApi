using Microsoft.AspNetCore.Identity;

namespace Portfolio.Infra.Identity;

/// <summary>
/// Represents an application user managed by ASP.NET Identity.
/// </summary>
public sealed class ApplicationUser : IdentityUser
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string FullName { get; set; }
}