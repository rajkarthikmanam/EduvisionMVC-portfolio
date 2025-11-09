using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using EduvisionMvc.Models;

namespace EduvisionMvc.Services;

public class LoginRedirectService : ILoginRedirectService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUrlHelperFactory _urlHelperFactory;

    public LoginRedirectService(UserManager<ApplicationUser> userManager, IUrlHelperFactory urlHelperFactory)
    {
        _userManager = userManager;
        _urlHelperFactory = urlHelperFactory;
    }

    public async Task<IActionResult> GetRedirectResultAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return new RedirectToActionResult("Index", "Home", null);
        }

        var roles = await _userManager.GetRolesAsync(user);

        // Redirect based on primary role (first role takes precedence)
        if (roles.Contains("Admin"))
        {
            return new RedirectToActionResult("Index", "AdminDashboard", null);
        }
        else if (roles.Contains("Instructor"))
        {
            return new RedirectToActionResult("Index", "InstructorDashboard", null);
        }
        else if (roles.Contains("Student"))
        {
            return new RedirectToActionResult("Index", "StudentDashboard", null);
        }

        // Default fallback
        return new RedirectToActionResult("Index", "Home", null);
    }
}
