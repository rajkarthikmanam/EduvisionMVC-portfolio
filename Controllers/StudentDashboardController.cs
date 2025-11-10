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
        if (user is null)
        {
            return Challenge();
        }
        // Load the student with all necessary related data. Use AsNoTracking for read-only to reduce change tracking overhead.
        var student = await _context.Students
            .Include(s => s.AdvisorInstructor)
            .Include(s => s.Enrollments)
                .ThenInclude(e => e.Course)
                    .ThenInclude(c => c!.Department)
            .Include(s => s.Enrollments)
                .ThenInclude(e => e.Course)
                    .ThenInclude(c => c!.CourseInstructors)
                        .ThenInclude(ci => ci.Instructor)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == user.Id);

        if (student == null)
        {
            // If the identity exists but no student profile yet, redirect to a flow that could create one (or show not found).
            return NotFound();
        }

        // Guard against null enrollment list (should never be null due to initialization) but handle gracefully.
    var enrollments = student!.Enrollments ?? new List<Enrollment>();

        // Filter enrollments ensuring Course is not null to avoid CS8602 warnings.
        var completedEnrollments = enrollments
            .Where(e => e.Numeric_Grade.HasValue && e.Course != null)
            .OrderByDescending(e => e.Numeric_Grade);

        var currentEnrollments = enrollments
            .Where(e => !e.Numeric_Grade.HasValue && e.Course != null)
            .OrderBy(e => e.Course!.Code);

        var model = new StudentDashboardViewModel
        {
            Name = student!.Name,
            Major = student!.Major,
            Gpa = student!.Gpa,
            AdvisorName = student!.AdvisorInstructor != null ? $"Dr. {student.AdvisorInstructor!.LastName}" : null,
            Phone = student!.Phone,
            AcademicLevel = student!.AcademicLevel,
            TotalCredits = completedEnrollments.Sum(e => e.Course!.Credits),
            CreditsInProgress = currentEnrollments.Sum(e => e.Course!.Credits),
            
            // Grade distribution
            GradeLabels = new() { "A", "B", "C", "D", "F" },
            GradeData = GetGradeDistribution(completedEnrollments),
            
            // Course performance
            CourseLabels = completedEnrollments.Select(e => e.Course!.Code).ToList(),
            CoursePerformance = completedEnrollments.Select(e => e.Numeric_Grade ?? 0).ToList(),
            
            // Course lists
            TopCourses = completedEnrollments
                .OrderByDescending(e => e.Numeric_Grade)
                .Take(3)
                .Select(e => new EnrollmentSummary
                {
                    CourseId = e.CourseId,
                    CourseCode = e.Course!.Code,
                    CourseTitle = e.Course!.Title,
                    Credits = e.Course!.Credits,
                    Term = e.Term,
                    Grade = e.Numeric_Grade,
                    InstructorName = e.Course!.CourseInstructors
                        .Select(ci => $"Dr. {ci.Instructor!.LastName}")
                        .FirstOrDefault() ?? "TBA",
                    Department = e.Course!.Department!.Name
                }).ToList(),

            WeakCourses = completedEnrollments
                .OrderBy(e => e.Numeric_Grade)
                .Take(3)
                .Select(e => new EnrollmentSummary
                {
                    CourseId = e.CourseId,
                    CourseCode = e.Course!.Code,
                    CourseTitle = e.Course!.Title,
                    Credits = e.Course!.Credits,
                    Term = e.Term,
                    Grade = e.Numeric_Grade,
                    InstructorName = e.Course!.CourseInstructors
                        .Select(ci => $"Dr. {ci.Instructor!.LastName}")
                        .FirstOrDefault() ?? "TBA",
                    Department = e.Course!.Department!.Name
                }).ToList(),

            CurrentCourses = currentEnrollments
                .Select(e => new EnrollmentSummary
                {
                    CourseId = e.CourseId,
                    CourseCode = e.Course!.Code,
                    CourseTitle = e.Course!.Title,
                    Credits = e.Course!.Credits,
                    Term = e.Term,
                    InstructorName = e.Course!.CourseInstructors
                        .Select(ci => $"Dr. {ci.Instructor!.LastName}")
                        .FirstOrDefault() ?? "TBA",
                    Department = e.Course!.Department!.Name
                }).ToList(),
            AllEnrollments = enrollments.ToList(),

            RecentNotifications = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync(),

            // Radar Chart - Skill competencies by subject area
            SkillRadarData = ComputeSkillRadarData(completedEnrollments)
        };

        return View(model);
    }

    private static RadarChartData ComputeSkillRadarData(IEnumerable<Enrollment> completedEnrollments)
    {
        // Group courses by department/subject area and compute average performance
        var departmentGroups = completedEnrollments
            .Where(e => e.Numeric_Grade.HasValue && e.Course != null && e.Course.Department != null)
            .GroupBy(e => e.Course!.Department!.Code)
            .Select(g => new
            {
                Department = g.Key,
                AverageGrade = g.Average(e => e.Numeric_Grade!.Value)
            })
            .OrderBy(x => x.Department)
            .ToList();

        if (!departmentGroups.Any())
        {
            return new RadarChartData
            {
                Labels = new() { "No Data" },
                Values = new() { 0 },
                DatasetLabel = "Subject Competency"
            };
        }

        return new RadarChartData
        {
            Labels = departmentGroups.Select(g => g.Department).ToList(),
            Values = departmentGroups.Select(g => g.AverageGrade).ToList(),
            DatasetLabel = "Subject Competency (GPA)"
        };
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