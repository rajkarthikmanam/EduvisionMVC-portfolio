using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduvisionMvc.Data;
using EduvisionMvc.Models;
using EduvisionMvc.Utilities;

namespace EduvisionMvc.Controllers;

[Authorize]
[ApiController]
[Route("api/dashboard")] 
public class DashboardApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardApiController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet("instructor")] 
    [Authorize(Roles = "Instructor,Admin")]
    public async Task<IActionResult> GetInstructor()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var instructor = await _db.Instructors
            .Include(i => i.CourseInstructors)
                .ThenInclude(ci => ci.Course)
                    .ThenInclude(c => c!.Enrollments)
            .FirstOrDefaultAsync(i => i.UserId == user.Id);

        if (instructor == null) return NotFound(new { message = "Instructor profile not found" });

        var currentTerm = AcademicTermHelper.GetCurrentTerm(DateTime.UtcNow);

        var allCourses = instructor.CourseInstructors
            .Select(ci => ci.Course)
            .Where(c => c != null)
            .Select(c => c!)
            .Distinct()
            .ToList();

        var currentCourses = allCourses
            .Where(c => c.Enrollments.Any(e => e.Term == currentTerm && e.Status != EnrollmentStatus.Dropped && e.Status != EnrollmentStatus.Rejected))
            .ToList();
        if (!currentCourses.Any()) currentCourses = allCourses;

        var activeStudents = currentCourses
            .SelectMany(c => c.Enrollments.Where(e => e.Term == currentTerm && e.Status != EnrollmentStatus.Dropped && e.Status != EnrollmentStatus.Rejected))
            .Select(e => e.StudentId)
            .Distinct()
            .Count();

        var labels = currentCourses.Select(c => c.Code).ToList();
        var counts = currentCourses
            .Select(c => c.Enrollments.Count(e => e.Term == currentTerm && e.Status != EnrollmentStatus.Dropped && e.Status != EnrollmentStatus.Rejected))
            .ToList();

        // Capacity average across currentCourses
        double capacityAvg = 0;
        if (currentCourses.Any())
        {
            capacityAvg = currentCourses
                .Select(c => new {
                    cap = Math.Max(1, c.Capacity),
                    filled = c.Enrollments.Count(e => e.Term == currentTerm && (e.Status == EnrollmentStatus.Approved || e.Status == EnrollmentStatus.Completed))
                })
                .Select(x => (double)x.filled / x.cap * 100.0)
                .Average();
        }

        return Ok(new
        {
            currentCoursesCount = currentCourses.Count,
            activeStudents,
            courseLabels = labels,
            enrollmentCounts = counts,
            capacityAvg = Math.Round(capacityAvg)
        });
    }

    [HttpGet("student")] 
    [Authorize(Roles = "Student,Admin")]
    public async Task<IActionResult> GetStudent()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var student = await _db.Students
            .Include(s => s.Enrollments)
                .ThenInclude(e => e.Course)
            .FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (student == null) return NotFound(new { message = "Student profile not found" });

        var nowTerm = AcademicTermHelper.GetCurrentTerm(DateTime.UtcNow);

        var currentEnrollments = student.Enrollments
            .Where(e => e.Term == nowTerm && e.Status != EnrollmentStatus.Dropped)
            .ToList();

        var completedEnrollments = student.Enrollments
            .Where(e => e.Numeric_Grade.HasValue && e.Status != EnrollmentStatus.Dropped)
            .ToList();

        var totalCredits = completedEnrollments.Sum(e => e.Course?.Credits ?? 0);
        var creditsInProgress = currentEnrollments.Where(e => !e.Numeric_Grade.HasValue).Sum(e => e.Course?.Credits ?? 0);

        // Build historical aggregates by term similar to the view
        var history = student.Enrollments
            .Where(e => e.Term != null)
            .GroupBy(e => e.Term!)
            .Select(g => new {
                term = g.Key,
                courseCount = g.Count(),
                totalCredits = g.Sum(e => e.Course != null ? e.Course.Credits : 0)
            })
            .OrderBy(h => h.term)
            .ToList();

        return Ok(new
        {
            currentCoursesCount = currentEnrollments.Count(e => !e.Numeric_Grade.HasValue),
            completedCourses = completedEnrollments.Count,
            totalCredits,
            creditsInProgress,
            requiredCredits = student.TotalCredits, // Credits required from Student table
            history
        });
    }

    [HttpGet("admin/trend")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdminEnrollmentTrend()
    {
        // Aggregate enrollments by term; order chronologically by Year then Season Spring, Summer, Fall
        var aggregates = await _db.Enrollments
            .Where(e => !string.IsNullOrEmpty(e.Term))
            .GroupBy(e => e.Term)
            .Select(g => new { term = g.Key!, count = g.Count() })
            .ToListAsync();

        int SeasonOrder(string term)
        {
            return term.Contains("Spring", StringComparison.OrdinalIgnoreCase) ? 1 :
                   term.Contains("Summer", StringComparison.OrdinalIgnoreCase) ? 2 : 3; // Fall default
        }

        var ordered = aggregates
            .Select(a => new
            {
                a.term,
                a.count,
                year = int.TryParse(a.term.Split(' ').LastOrDefault(), out var y) ? y : 0,
                season = SeasonOrder(a.term)
            })
            .OrderBy(x => x.year)
            .ThenBy(x => x.season)
            .ToList();

        var labels = ordered.Select(x => x.term).ToList();
        var data = ordered.Select(x => x.count).ToList();
        return Ok(new { labels, data });
    }

    [HttpGet("admin/capacity")] 
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdminCapacity()
    {
        var term = Utilities.AcademicTermHelper.GetCurrentTerm(DateTime.UtcNow);
        var items = await _db.Courses
            .Include(c => c.Department)
            .Select(c => new {
                code = c.Code,
                title = c.Title,
                dept = c.Department != null ? c.Department.Code : "N/A",
                capacity = c.Capacity,
                current = c.Enrollments.Count(e => e.Term == term && (e.Status == EnrollmentStatus.Approved || e.Status == EnrollmentStatus.Pending))
            })
            .ToListAsync();
        var top = items.OrderByDescending(i => i.current).Take(10).ToList();
        var alerts = items
            .Where(i => i.capacity > 0 && (i.current * 100 / i.capacity) >= 80)
            .OrderByDescending(i => i.current * 100 / i.capacity)
            .Take(10)
            .Select(i => new { i.code, i.title, i.capacity, i.current, util = i.capacity == 0 ? 0 : Math.Round((double)i.current / i.capacity * 100) })
            .ToList();
        return Ok(new {
            labels = top.Select(i => i.code).ToList(),
            current = top.Select(i => i.current).ToList(),
            capacity = top.Select(i => i.capacity).ToList(),
            alerts
        });
    }
    
    [HttpGet("admin/departments")] 
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdminDepartments()
    {
        var data = await _db.Courses
            .Include(c => c.Department)
            .GroupBy(c => c.Department != null ? c.Department.Code : "N/A")
            .Select(g => new { dept = g.Key, count = g.Count() })
            .ToListAsync();
        return Ok(data);
    }
}
