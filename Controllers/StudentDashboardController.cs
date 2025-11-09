using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduvisionMvc.Data;
using EduvisionMvc.Models;
using EduvisionMvc.ViewModels;

namespace EduvisionMvc.Controllers;

[Authorize(Roles = "Student")]
public class StudentDashboardController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public StudentDashboardController(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var student = await _context.Students
            .Include(s => s.Enrollments)
                .ThenInclude(e => e.Course)
                    .ThenInclude(c => c.Department)
            .Include(s => s.Enrollments)
                .ThenInclude(e => e.Course)
                    .ThenInclude(c => c.CourseInstructors)
                        .ThenInclude(ci => ci.Instructor)
            .FirstOrDefaultAsync(s => s.UserId == user.Id);

        if (student == null) return NotFound();

        var completedEnrollments = student.Enrollments
            .Where(e => e.Numeric_Grade.HasValue)
            .OrderByDescending(e => e.Numeric_Grade);

        var currentEnrollments = student.Enrollments
            .Where(e => !e.Numeric_Grade.HasValue)
            .OrderBy(e => e.Course.Code);

        var model = new StudentDashboardViewModel
        {
            Name = student.Name,
            Major = student.Major,
            Gpa = student.Gpa,
            TotalCredits = completedEnrollments.Sum(e => e.Course.Credits),
            CreditsInProgress = currentEnrollments.Sum(e => e.Course.Credits),
            
            // Grade distribution
            GradeLabels = new() { "A", "B", "C", "D", "F" },
            GradeData = GetGradeDistribution(completedEnrollments),
            
            // Course performance
            CourseLabels = completedEnrollments.Select(e => e.Course.Code).ToList(),
            CoursePerformance = completedEnrollments.Select(e => e.Numeric_Grade ?? 0).ToList(),
            
            // Course lists
            TopCourses = completedEnrollments
                .OrderByDescending(e => e.Numeric_Grade)
                .Take(3)
                .Select(e => new EnrollmentSummary
                {
                    CourseId = e.CourseId,
                    CourseCode = e.Course.Code,
                    CourseTitle = e.Course.Title,
                    Credits = e.Course.Credits,
                    Term = e.Term,
                    Grade = e.Numeric_Grade,
                    InstructorName = e.Course.CourseInstructors
                        .Select(ci => $"Dr. {ci.Instructor.LastName}")
                        .FirstOrDefault() ?? "TBA",
                    Department = e.Course.Department.Name
                }).ToList(),

            WeakCourses = completedEnrollments
                .OrderBy(e => e.Numeric_Grade)
                .Take(3)
                .Select(e => new EnrollmentSummary
                {
                    CourseId = e.CourseId,
                    CourseCode = e.Course.Code,
                    CourseTitle = e.Course.Title,
                    Credits = e.Course.Credits,
                    Term = e.Term,
                    Grade = e.Numeric_Grade,
                    InstructorName = e.Course.CourseInstructors
                        .Select(ci => $"Dr. {ci.Instructor.LastName}")
                        .FirstOrDefault() ?? "TBA",
                    Department = e.Course.Department.Name
                }).ToList(),

            CurrentCourses = currentEnrollments
                .Select(e => new EnrollmentSummary
                {
                    CourseId = e.CourseId,
                    CourseCode = e.Course.Code,
                    CourseTitle = e.Course.Title,
                    Credits = e.Course.Credits,
                    Term = e.Term,
                    InstructorName = e.Course.CourseInstructors
                        .Select(ci => $"Dr. {ci.Instructor.LastName}")
                        .FirstOrDefault() ?? "TBA",
                    Department = e.Course.Department.Name
                }).ToList(),

            RecentNotifications = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync()
        };

        return View(model);
    }

    private static List<decimal> GetGradeDistribution(IEnumerable<Enrollment> enrollments)
    {
        var distribution = new decimal[5]; // A, B, C, D, F

        foreach (var enrollment in enrollments.Where(e => e.Numeric_Grade.HasValue))
        {
            var grade = enrollment.Numeric_Grade!.Value;
            if (grade >= 3.7m) distribution[0]++;      // A
            else if (grade >= 2.7m) distribution[1]++; // B
            else if (grade >= 1.7m) distribution[2]++; // C
            else if (grade >= 0.7m) distribution[3]++; // D
            else distribution[4]++;                     // F
        }

        return distribution.ToList();
    }
}