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
        // Ensure a Student profile exists for this identity (auto-heal here as a backup)
        var existing = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (existing == null)
        {
            var dept = await _context.Departments.OrderBy(d => d.Id).FirstOrDefaultAsync();
            if (dept == null)
            {
                dept = new Department { Name = "General" };
                _context.Departments.Add(dept);
                await _context.SaveChangesAsync();
            }
            existing = new Student
            {
                UserId = user.Id,
                Name = ($"{user.FirstName} {user.LastName}").Trim(),
                Email = user.Email ?? user.UserName ?? string.Empty,
                Major = dept.Name,
                DepartmentId = dept.Id,
                EnrollmentDate = DateTime.UtcNow.Date,
                Gpa = 0m,
                TotalCredits = 120 // Credits required for graduation
            };
            _context.Students.Add(existing);
            await _context.SaveChangesAsync();
            user.StudentId = existing.Id;
            await _userManager.UpdateAsync(user);
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

        if (student == null) return NotFound();

        // Guard against null enrollment list (should never be null due to initialization) but handle gracefully.
    var enrollments = student!.Enrollments ?? new List<Enrollment>();

        // Filter enrollments ensuring Course is not null to avoid CS8602 warnings.
        var completedEnrollments = enrollments
            .Where(e => e.Numeric_Grade.HasValue && e.Course != null && e.Status != EnrollmentStatus.Dropped)
            .OrderByDescending(e => e.Numeric_Grade);

        // Current enrollments: Approved only (actively taking)
        var currentEnrollments = enrollments
            .Where(e => !e.Numeric_Grade.HasValue && e.Course != null && e.Status == EnrollmentStatus.Approved)
            .OrderBy(e => e.Course!.Code);

        // Pending enrollments: Waiting for approval
        var pendingEnrollments = enrollments
            .Where(e => !e.Numeric_Grade.HasValue && e.Course != null && e.Status == EnrollmentStatus.Pending)
            .OrderBy(e => e.Course!.Code);

        // Recalculate GPA: only graded, non-dropped enrollments
        var gradedEnrollments = completedEnrollments.ToList();
        var recalculatedGpa = gradedEnrollments.Any()
            ? gradedEnrollments.Average(e => e.Numeric_Grade!.Value)
            : 0m;

        // Calculate completed credits from graded enrollments
        var completedCredits = gradedEnrollments.Sum(e => e.Course!.Credits);

        // If no completed (graded) credits, force GPA display to 0.00 even if legacy value persisted
        if (!gradedEnrollments.Any())
        {
            student.Gpa = 0m; // ensure any downstream usage shows zero
        }

        // Group enrollments by term for the enrollment trend chart (all 3 terms)
        // Ensure all 3 terms are included even if they have 0 enrollments
        var allTerms = new[] { "Spring 2025", "Summer 2025", "Fall 2025" };
        var termEnrollments = enrollments
            .Where(e => e.Course != null && e.Status != EnrollmentStatus.Dropped)
            .GroupBy(e => e.Term)
            .ToDictionary(g => g.Key, g => new { Count = g.Count(), Credits = g.Sum(e => e.Course!.Credits) });
        
        var termGroups = allTerms
            .Select(term => new { 
                Term = term, 
                Count = termEnrollments.ContainsKey(term) ? termEnrollments[term].Count : 0,
                Credits = termEnrollments.ContainsKey(term) ? termEnrollments[term].Credits : 0
            })
            .ToList();

        var model = new StudentDashboardViewModel
        {
            Name = student!.Name,
            Major = student!.Major,
            Gpa = recalculatedGpa,
            CompletedCoursesCount = gradedEnrollments.Count,
            AdvisorName = student!.AdvisorInstructor != null ? $"Dr. {student.AdvisorInstructor!.LastName}" : null,
            Phone = student!.Phone,
            AcademicLevel = student!.AcademicLevel,
            TotalCredits = completedCredits, // Completed credits from enrollments
            CreditsInProgress = currentEnrollments.Sum(e => e.Course!.Credits),
            RequiredCredits = student.TotalCredits, // Total credits required (from Student table)
            
            // Grade distribution
            GradeLabels = new() { "A", "B", "C", "D", "F" },
            GradeData = GetGradeDistribution(completedEnrollments),
            
            // Course performance - show all 3 terms
            CourseLabels = termGroups.Select(t => t.Term).ToList(),
            CoursePerformance = termGroups.Select(t => (decimal)t.Count).ToList(),
            
            // Top Performance: Only the single highest grade course
            TopCourses = completedEnrollments
                .OrderByDescending(e => e.Numeric_Grade)
                .Take(1) // Only show THE top course
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

            // Areas for Improvement: Courses with grade <= 2.0
            WeakCourses = completedEnrollments
                .Where(e => e.Numeric_Grade <= 2.0m)
                .OrderBy(e => e.Numeric_Grade)
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

            // All Completed Courses: All courses with grades
            CompletedCourses = completedEnrollments
                .OrderByDescending(e => e.Term)
                .ThenBy(e => e.Course!.Code)
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

            PendingCourses = pendingEnrollments
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