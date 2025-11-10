using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduvisionMvc.Data;
using EduvisionMvc.Models;
using EduvisionMvc.ViewModels;

namespace EduvisionMvc.Controllers;

[Authorize(Roles = "Admin")]
public class AdminDashboardController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminDashboardController(AppDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        // Aggregate counts
        var totalUsers = await _userManager.Users.CountAsync();
        var totalStudents = await _db.Students.CountAsync();
        var totalInstructors = await _db.Instructors.CountAsync();
        var totalCourses = await _db.Courses.CountAsync();
        var activeEnrollments = await _db.Enrollments.CountAsync(e => e.Numeric_Grade == null);
        var avgGpa = await _db.Students.Where(s => s.Gpa > 0).Select(s => s.Gpa).DefaultIfEmpty().AverageAsync();
        var materialsCount = await _db.CourseMaterials.CountAsync();
        var discussionsCount = await _db.Discussions.CountAsync();
        var assignmentsCount = await _db.Assignments.CountAsync();

        // Roles distribution
        var roleDistribution = new List<RoleCount>();
        foreach (var role in _roleManager.Roles)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            roleDistribution.Add(new RoleCount { Role = role.Name!, Count = usersInRole.Count });
        }

        // Capacity alerts (courses >= 80% full)
        var capacityAlerts = await _db.Courses
            .Include(c => c.Enrollments)
            .Where(c => c.Capacity > 0)
            .Select(c => new CourseCapacitySummary
            {
                CourseId = c.Id,
                Code = c.Code,
                Title = c.Title,
                Capacity = c.Capacity,
                Current = c.Enrollments.Count(e => e.Numeric_Grade == null)
            })
            .Where(c => c.Current * 100 / c.Capacity >= 80)
            .OrderByDescending(c => c.Current * 100 / c.Capacity)
            .Take(10)
            .ToListAsync();

        // Recent notifications (last 10)
        var recentNotifications = await _db.Notifications
            .Include(n => n.User)
            .OrderByDescending(n => n.CreatedAt)
            .Take(10)
            .Select(n => new RecentNotificationSummary
            {
                UserEmail = n.User!.Email ?? n.User!.UserName ?? "(unknown)",
                Message = n.Message,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        // Scatter chart data: Course Capacity vs Current Enrollment
        var courseCapacityData = await _db.Courses
            .Where(c => c.Capacity > 0)
            .Select(c => new ScatterDataPoint
            {
                CourseCode = c.Code,
                X = c.Capacity,
                Y = c.Enrollments.Count(e => e.Numeric_Grade == null),
                UtilizationRate = c.Capacity > 0 ? Math.Round((double)c.Enrollments.Count(e => e.Numeric_Grade == null) / c.Capacity * 100, 1) : 0
            })
            .ToListAsync();

        var vm = new AdminDashboardViewModel
        {
            TotalUsers = totalUsers,
            TotalStudents = totalStudents,
            TotalInstructors = totalInstructors,
            TotalCourses = totalCourses,
            ActiveEnrollments = activeEnrollments,
            AverageGpa = Math.Round(avgGpa, 2),
            MaterialsCount = materialsCount,
            DiscussionsCount = discussionsCount,
            AssignmentsCount = assignmentsCount,
            PendingApprovals = await _db.Enrollments.CountAsync(e => e.Status == EnrollmentStatus.Pending),
            RoleDistribution = roleDistribution,
            CapacityAlerts = capacityAlerts,
            RecentNotifications = recentNotifications,
            CourseCapacityData = courseCapacityData
        };

        return View(vm);
    }
}
