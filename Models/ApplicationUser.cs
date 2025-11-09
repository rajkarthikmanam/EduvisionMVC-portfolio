using Microsoft.AspNetCore.Identity;

namespace EduvisionMvc.Models;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateJoined { get; set; }
    public string? Avatar { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginDate { get; set; }

    // Navigation property for additional profile data
    public UserProfile? Profile { get; set; }

    // Calculated full name
    public string FullName => $"{FirstName} {LastName}".Trim();
}