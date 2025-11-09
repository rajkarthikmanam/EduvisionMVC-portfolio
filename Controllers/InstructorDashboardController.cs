using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduvisionMvc.Data;
using EduvisionMvc.Models;
using EduvisionMvc.ViewModels;

namespace EduvisionMvc.Controllers;

[Authorize(Roles = "Instructor")]
public class InstructorDashboardController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public InstructorDashboardController(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var instructor = await _context.Instructors
            .Include(i => i.Department)
            .Include(i => i.CourseInstructors)
                .ThenInclude(ci => ci.Course)
                    .ThenInclude(c => c.Enrollments)
                        .ThenInclude(e => e.Student)
            .FirstOrDefaultAsync(i => i.UserId == user.Id);

        if (instructor == null) return NotFound();

        var currentTerm = "Fall 2025"; // TODO: Make this dynamic
        var currentCourses = instructor.CourseInstructors
            .Select(ci => ci.Course)
            .Where(c => c.Enrollments.Any(e => e.Term == currentTerm));

        var pastCourses = instructor.CourseInstructors
            .Select(ci => ci.Course)
            .Where(c => c.Enrollments.Any(e => e.Term != currentTerm));

        var model = new InstructorDashboardViewModel
        {
            Name = $"Dr. {instructor.LastName}",
            Department = instructor.Department.Name,
            Email = instructor.Email,
            TotalCourses = instructor.CourseInstructors.Count,
            ActiveStudents = currentCourses
                .SelectMany(c => c.Enrollments)
                .Select(e => e.StudentId)
                .Distinct()
                .Count(),

            // Course enrollment chart
            CourseLabels = currentCourses.Select(c => c.Code).ToList(),
            EnrollmentCounts = currentCourses
                .Select(c => c.Enrollments.Count(e => e.Term == currentTerm))
                .ToList(),

            // Grade distribution chart
            GradeLabels = new() { "A", "B", "C", "D", "F" },
            GradeDistribution = GetGradeDistribution(currentCourses
                .SelectMany(c => c.Enrollments)
                .Where(e => e.Term == currentTerm && e.Numeric_Grade.HasValue)
                .Select(e => e.Numeric_Grade!.Value)),

            // Current courses with statistics
            CurrentCourses = currentCourses.Select(c => new CourseStatistics
            {
                CourseId = c.Id,
                Code = c.Code,
                Title = c.Title,
                Term = currentTerm,
                EnrollmentCount = c.Enrollments.Count(e => e.Term == currentTerm),
                AverageGrade = c.Enrollments
                    .Where(e => e.Term == currentTerm && e.Numeric_Grade.HasValue)
                    .Select(e => e.Numeric_Grade!.Value)
                    .DefaultIfEmpty()
                    .Average(),
                Credits = c.Credits,
                Schedule = "MWF 10:00-10:50", // TODO: Add schedule to Course model
                MaterialsCount = _context.CourseMaterials.Count(m => m.CourseId == c.Id)
            }).ToList(),

            // Past courses history
            PastCourses = pastCourses.Select(c => new CourseHistory
            {
                CourseId = c.Id,
                Code = c.Code,
                Title = c.Title,
                Term = c.Enrollments.First().Term,
                TotalStudents = c.Enrollments.Count,
                AverageGrade = c.Enrollments
                    .Where(e => e.Numeric_Grade.HasValue)
                    .Select(e => e.Numeric_Grade!.Value)
                    .DefaultIfEmpty()
                    .Average(),
                PassRate = c.Enrollments
                    .Count(e => e.Numeric_Grade >= 1.0m) * 100m / c.Enrollments.Count
            }).ToList(),

            // Top performing students
            TopStudents = currentCourses
                .SelectMany(c => c.Enrollments
                    .Where(e => e.Term == currentTerm)
                    .Select(e => e.Student))
                .GroupBy(s => s.Id)
                .Select(g => new StudentPerformance
                {
                    StudentId = g.Key,
                    Name = g.First().Name,
                    Major = g.First().Major,
                    GradeAverage = g.First().Gpa,
                    CoursesCompleted = g.First().Enrollments.Count(e => e.Numeric_Grade.HasValue),
                    CompletedCourses = g.First().Enrollments
                        .Where(e => e.Numeric_Grade.HasValue)
                        .Select(e => e.Course.Code)
                        .ToList()
                })
                .OrderByDescending(s => s.GradeAverage)
                .Take(5)
                .ToList(),

            // Recent notifications
            RecentNotifications = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync(),

            // Recent course materials
            RecentMaterials = await _context.CourseMaterials
                .Where(m => currentCourses.Select(c => c.Id).Contains(m.CourseId))
                .OrderByDescending(m => m.UploadedDate)
                .Take(5)
                .ToListAsync()
        };

        return View(model);
    }

    private static List<int> GetGradeDistribution(IEnumerable<decimal> grades)
    {
        var distribution = new int[5]; // A, B, C, D, F

        foreach (var grade in grades)
        {
            if (grade >= 3.7m) distribution[0]++;      // A
            else if (grade >= 2.7m) distribution[1]++; // B
            else if (grade >= 1.7m) distribution[2]++; // C
            else if (grade >= 0.7m) distribution[3]++; // D
            else distribution[4]++;                     // F
        }

        return distribution.ToList();
    }
}