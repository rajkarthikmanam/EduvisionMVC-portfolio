using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using EduvisionMvc.Models;

namespace EduvisionMvc.Controllers
{
    [Route("dev/admin")]
    [AllowAnonymous]
    public class DevAdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DevAdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: /dev/admin/ensure-admin
        // Ensures Admin role and a default admin@local.test user (dev only)
        [HttpGet("ensure-admin")]
        public async Task<IActionResult> EnsureAdmin()
        {
            var roleName = "Admin";

            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await _roleManager.CreateAsync(new IdentityRole(roleName));
                if (!roleResult.Succeeded)
                {
                    return Json(new { Status = "Error", Message = "Failed creating Admin role", Errors = roleResult.Errors });
                }
            }

            var email = "admin@local.test";
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = "Admin",
                    LastName = "User",
                    CreatedAt = System.DateTime.UtcNow
                };
                var createResult = await _userManager.CreateAsync(user, "Admin123!");
                if (!createResult.Succeeded)
                {
                    return Json(new { Status = "Error", Message = "Failed creating admin user", Errors = createResult.Errors });
                }
            }

            if (!await _userManager.IsInRoleAsync(user, roleName))
            {
                var addRoleResult = await _userManager.AddToRoleAsync(user, roleName);
                if (!addRoleResult.Succeeded)
                {
                    return Json(new { Status = "Error", Message = "Failed adding admin role", Errors = addRoleResult.Errors });
                }
            }

            return Json(new { Status = "Success", Message = "Admin user ensured", Email = email });
        }
    }
}
