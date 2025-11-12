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
        var activeEnrollments = await _db.Enrollments.CountAsync(e => e.Term == "Fall 2025" && (e.Status == EnrollmentStatus.Approved || e.Status == EnrollmentStatus.Pending) && e.NumericGrade == null);
        var avgGpa = await _db.Students.Where(s => s.Gpa > 0).Select(s => s.Gpa).DefaultIfEmpty().AverageAsync();
        var materialsCount = 0; // CourseMaterials table doesn't exist yet
        var discussionsCount = 0; // Discussions table doesn't exist yet
        var assignmentsCount = 0; // Assignments table doesn't exist yet

        // Roles distribution
        var roleDistribution = new List<RoleCount>();
        foreach (var role in _roleManager.Roles)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            roleDistribution.Add(new RoleCount { Role = role.Name!, Count = usersInRole.Count });
        }

        // Pre-compute active enrollment counts per course
        var activeByCourse = await _db.Enrollments
            .Where(e => e.Term == "Fall 2025" && (e.Status == EnrollmentStatus.Approved || e.Status == EnrollmentStatus.Pending))
            .GroupBy(e => e.CourseId)
            .Select(g => new { CourseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CourseId, x => x.Count);

        // Base course info
        var courses = await _db.Courses
            .Include(c => c.Department)
            .Select(c => new { c.Id, c.Code, c.Title, c.Capacity, DeptCode = c.Department != null ? c.Department.Code : "N/A" })
            .ToListAsync();

        // Capacity alerts (>=80%) computed in-memory
        var capacityAlerts = courses
            .Where(c => c.Capacity > 0)
            .Select(c => new CourseCapacitySummary
            {
                CourseId = c.Id,
                Code = c.Code,
                Title = c.Title,
                Capacity = c.Capacity,
                Current = activeByCourse.TryGetValue(c.Id, out var cnt) ? cnt : 0
            })
            .Where(c => c.Capacity > 0 && (c.Current * 100 / c.Capacity) >= 80)
            .OrderByDescending(c => c.Current * 100 / c.Capacity)
            .Take(10)
            .ToList();

        // Recent notifications (last 10) - table doesn't exist yet
        var recentNotifications = new List<RecentNotificationSummary>();

        // Scatter chart data computed in-memory
        var courseCapacityData = courses
            .Where(c => c.Capacity > 0)
            .Select(c =>
            {
                var current = activeByCourse.TryGetValue(c.Id, out var cnt) ? cnt : 0;
                var util = c.Capacity > 0 ? Math.Round((double)current / c.Capacity * 100.0, 1) : 0.0;
                return new ScatterDataPoint
                {
                    CourseCode = c.Code,
                    DepartmentCode = c.DeptCode,
                    X = c.Capacity,
                    Y = current,
                    UtilizationRate = util
                };
            })
            .ToList();

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
