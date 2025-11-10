using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using EduvisionMvc.Models;

namespace EduvisionMvc.Authorization;

/// <summary>
/// Prevents deletion or demotion of admin accounts. 
/// Only admins can modify users, but they cannot delete other admins or remove admin roles.
/// This prevents system lockout scenarios.
/// </summary>
public class AdminProtectionHandler : AuthorizationHandler<AdminProtectionRequirement, ApplicationUser>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminProtectionHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminProtectionRequirement requirement,
        ApplicationUser targetUser)
    {
        // Check if the target user is an admin
        var isTargetAdmin = await _userManager.IsInRoleAsync(targetUser, "Admin");

        if (!isTargetAdmin)
        {
            // Not an admin, allow operation
            context.Succeed(requirement);
            return;
        }

        // Target is an admin - check if current user is also an admin
        var currentUser = context.User;
        if (!currentUser.IsInRole("Admin"))
        {
            // Non-admins cannot modify admin accounts
            context.Fail();
            return;
        }

        // Count total admins in the system
        var allAdmins = await _userManager.GetUsersInRoleAsync("Admin");
        
        if (allAdmins.Count == 1)
        {
            // This is the last admin - prevent deletion/demotion
            context.Fail();
            return;
        }

        // Multiple admins exist and current user is admin - allow operation
        context.Succeed(requirement);
    }
}
