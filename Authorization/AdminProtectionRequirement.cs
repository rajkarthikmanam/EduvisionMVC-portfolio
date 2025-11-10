using Microsoft.AspNetCore.Authorization;

namespace EduvisionMvc.Authorization;

/// <summary>
/// Authorization requirement to protect admin accounts from deletion or role changes
/// </summary>
public class AdminProtectionRequirement : IAuthorizationRequirement
{
    public AdminProtectionRequirement() { }
}
