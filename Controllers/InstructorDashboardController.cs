using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduvisionMvc.Data;
using EduvisionMvc.Models;
using EduvisionMvc.ViewModels;
using EduvisionMvc.Utilities;

namespace EduvisionMvc.Controllers;

[Authorize(Roles = "Admin,Instructor")]
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
                    .ThenInclude(c => c!.Enrollments)
                        .ThenInclude(e => e.Student)
            .FirstOrDefaultAsync(i => i.UserId == user!.Id);

        if (instructor == null)
        {
            if (User.IsInRole("Admin"))
            {
                TempData["Error"] = "No instructor profile linked. Open an instructor from the list to manage courses.";
                return RedirectToAction("Index", "Instructors");
            }
            TempData["Error"] = "Instructor profile missing. Contact an administrator.";
            return RedirectToAction("Index", "Home");
        }

        var currentTerm = AcademicTermHelper.GetCurrentTerm(DateTime.UtcNow);

        // All assigned courses
        var allInstructorCourses = instructor.CourseInstructors
            .Select(ci => ci.Course)
            .Where(c => c != null)
            .Select(c => c!)
            .Distinct()
            .ToList();

        // Current-term active courses: must have at least one enrollment in current term that isn't Dropped/Rejected
        var currentCourses = allInstructorCourses
            .Where(c => c.Enrollments.Any(e => e.Term == currentTerm && e.Status != EnrollmentStatus.Dropped && e.Status != EnrollmentStatus.Rejected))
            .ToList();

        // If none match (e.g., term just started), fall back to all assigned so dashboard isn't empty
        if (!currentCourses.Any()) currentCourses = allInstructorCourses;

        // Past courses: distinct courses with at least one graded enrollment from a previous term
        var pastCourses = allInstructorCourses
            .Where(c => c.Enrollments.Any(e => e.Term != currentTerm && e.NumericGrade.HasValue && e.Status != EnrollmentStatus.Dropped))
            .Distinct()
            .ToList();

        // Pending enrollments count
        var pendingCount = allInstructorCourses
            .SelectMany(c => c.Enrollments)
            .Count(e => e.Status == EnrollmentStatus.Pending && e.Term == currentTerm);

        // Active student IDs (current term, valid statuses only)
        var activeStudentIds = currentCourses
            .SelectMany(c => c.Enrollments.Where(e => e.Term == currentTerm && e.Status != EnrollmentStatus.Dropped && e.Status != EnrollmentStatus.Rejected))
            .Select(e => e.StudentId)
            .Distinct()
            .ToList();

        var model = new InstructorDashboardViewModel
        {
            InstructorId = instructor.Id,
            Name = $"Dr. {instructor.LastName}",
            Department = instructor.Department?.Name ?? "",
            Email = instructor.Email,
            TotalCourses = allInstructorCourses.Count,
            ActiveStudents = activeStudentIds.Count,
            PendingApprovalsCount = pendingCount,
            CourseLabels = currentCourses.Select(c => c.Code).ToList(),
            EnrollmentCounts = currentCourses
                .Select(c => c.Enrollments.Count(e => e.Term == currentTerm && e.Status != EnrollmentStatus.Dropped && e.Status != EnrollmentStatus.Rejected))
                .ToList(),
            CurrentCourses = currentCourses.Select(c => new CourseStatistics
            {
                CourseId = c.Id,
                Code = c.Code,
                Title = c.Title,
                Term = currentTerm,
                EnrollmentCount = c.Enrollments.Count(e => e.Term == currentTerm && e.Status != EnrollmentStatus.Dropped && e.Status != EnrollmentStatus.Rejected),
                Capacity = c.Capacity,
                AverageGrade = c.Enrollments
                    .Where(e => e.Term == currentTerm && e.NumericGrade.HasValue && e.Status != EnrollmentStatus.Dropped && e.Status != EnrollmentStatus.Rejected)
                    .Select(e => e.NumericGrade!.Value)
                    .DefaultIfEmpty()
                    .Average(),
                Credits = c.Credits,
                Schedule = c.Schedule ?? "TBA",
                MaterialsCount = _context.CourseMaterials.Count(m => m.CourseId == c.Id)
            }).ToList(),
            PastCourses = pastCourses.Select(c => new CourseHistory
            {
                CourseId = c.Id,
                Code = c.Code,
                Title = c.Title,
                Term = c.Enrollments.Where(e => e.NumericGrade.HasValue && e.Term != currentTerm).Select(e => e.Term).FirstOrDefault() ?? currentTerm,
                TotalStudents = c.Enrollments.Count(e => e.NumericGrade.HasValue && e.Status != EnrollmentStatus.Dropped),
                AverageGrade = c.Enrollments
                    .Where(e => e.NumericGrade.HasValue && e.Status != EnrollmentStatus.Dropped)
                    .Select(e => e.NumericGrade!.Value)
                    .DefaultIfEmpty()
                    .Average(),
                PassRate = (c.Enrollments.Count(e => e.NumericGrade.HasValue && e.Status != EnrollmentStatus.Dropped) > 0)
                    ? (c.Enrollments.Count(e => e.NumericGrade.HasValue && e.NumericGrade!.Value >= 1.0m && e.Status != EnrollmentStatus.Dropped) * 100m /
                        c.Enrollments.Count(e => e.NumericGrade.HasValue && e.Status != EnrollmentStatus.Dropped))
                    : 0m
            }).ToList(),
            PendingApprovals = currentCourses
                .SelectMany(c => c.Enrollments.Where(e => e.Status == EnrollmentStatus.Pending && e.Term == currentTerm))
                .Select(e => new PendingEnrollment
                {
                    EnrollmentId = e.Id,
                    StudentId = e.StudentId,
                    StudentName = e.Student!.Name,
                    CourseCode = e.Course!.Code,
                    CourseTitle = e.Course!.Title,
                    Term = e.Term,
                    EnrollDate = e.EnrolledDate
                })
                .OrderBy(p => p.EnrollDate)
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ApproveEnrollment(int id)
    {
        var enrollment = await _context.Enrollments
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (enrollment == null)
        {
            return NotFound();
        }

        // Verify instructor owns this course
        var user = await _userManager.GetUserAsync(User);
        var instructor = await _context.Instructors
            .Include(i => i.CourseInstructors)
            .FirstOrDefaultAsync(i => i.UserId == user!.Id);

        if (instructor == null || !instructor.CourseInstructors.Any(ci => ci.CourseId == enrollment.CourseId))
        {
            TempData["Error"] = "You do not have permission to approve this enrollment.";
            return RedirectToAction(nameof(Index));
        }

        enrollment.Status = EnrollmentStatus.Approved;
        enrollment.ApprovedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Enrollment approved successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeclineEnrollment(int id)
    {
        var enrollment = await _context.Enrollments
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (enrollment == null)
        {
            return NotFound();
        }

        // Verify instructor owns this course
        var user = await _userManager.GetUserAsync(User);
        var instructor = await _context.Instructors
            .Include(i => i.CourseInstructors)
            .FirstOrDefaultAsync(i => i.UserId == user!.Id);

        if (instructor == null || !instructor.CourseInstructors.Any(ci => ci.CourseId == enrollment.CourseId))
        {
            TempData["Error"] = "You do not have permission to decline this enrollment.";
            return RedirectToAction(nameof(Index));
        }

        _context.Enrollments.Remove(enrollment);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Enrollment declined and removed.";
        return RedirectToAction(nameof(Index));
    }
}
