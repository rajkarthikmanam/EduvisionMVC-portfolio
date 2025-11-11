using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduvisionMvc.Data;
using EduvisionMvc.Models;

namespace EduvisionMvc.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationsController(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpPost]
    public async Task<IActionResult> MarkAllRead()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var notifications = await _context.Notifications
            .Where(n => n.UserId == user.Id && !n.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync();

        return RedirectToAction("Index", GetDashboardController());
    }

    [HttpPost]
    public async Task<IActionResult> MarkRead(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id);

        if (notification != null)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Index", GetDashboardController());
    }

    private string GetDashboardController()
    {
        if (User.IsInRole("Student")) return "StudentDashboard";
        if (User.IsInRole("Instructor")) return "InstructorDashboard";
        if (User.IsInRole("Admin")) return "AdminDashboard";
        return "Home";
    }
}
