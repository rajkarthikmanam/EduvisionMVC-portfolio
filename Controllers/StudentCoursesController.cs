using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduvisionMvc.Data;
using EduvisionMvc.Models;
using EduvisionMvc.ViewModels;

namespace EduvisionMvc.Controllers;

[Authorize(Roles="Student")]
public class StudentCoursesController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public StudentCoursesController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // Shows courses the student is NOT yet enrolled in for the current term
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var student = await _db.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (student == null) return RedirectToAction("Index", "StudentDashboard");

        var currentTerm = GetCurrentTerm();

        var enrolledCourseIds = await _db.Enrollments
            .Where(e => e.StudentId == student.Id && e.Term == currentTerm)
            .Select(e => e.CourseId)
            .ToListAsync();

        var query = _db.Courses
            .Include(c => c.Department)
            .Where(c => !enrolledCourseIds.Contains(c.Id));

        if (student.DepartmentId.HasValue)
        {
            query = query.Where(c => c.DepartmentId == student.DepartmentId);
        }

        // Load enrollments for capacity calculation (current term only)
        var availableRaw = await query
            .Include(c => c.Enrollments)
            .OrderBy(c => c.Code)
            .ToListAsync();

        var options = availableRaw.Select(c => new CourseEnrollOption
        {
            Course = c,
            CurrentEnrollments = c.Enrollments.Count(e => e.Term == currentTerm && (e.Status == EnrollmentStatus.Approved || e.Status == EnrollmentStatus.Completed))
        }).ToList();

        ViewData["CurrentTerm"] = currentTerm;
        return View(options);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll(int courseId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        var student = await _db.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (student == null) return RedirectToAction("Index", "StudentDashboard");

        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
        if (course == null) return NotFound();

        var term = GetCurrentTerm();

        // Prevent duplicate enrollment
        var exists = await _db.Enrollments.AnyAsync(e => e.StudentId == student.Id && e.CourseId == courseId && e.Term == term);
        if (exists)
        {
            TempData["EnrollMessage"] = $"Already enrolled in {course.Code} for {term}.";
            return RedirectToAction("Index");
        }

        // Capacity check
        var currentCount = await _db.Enrollments.CountAsync(e => e.CourseId == courseId && e.Term == term && (e.Status == EnrollmentStatus.Approved || e.Status == EnrollmentStatus.Completed));
        if (currentCount >= course.Capacity)
        {
            TempData["EnrollMessage"] = $"Course {course.Code} is full.";
            return RedirectToAction("Index");
        }

        _db.Enrollments.Add(new Enrollment
        {
            StudentId = student.Id,
            CourseId = courseId,
            Term = term,
            Status = EnrollmentStatus.Approved,
            Numeric_Grade = null
        });
        await _db.SaveChangesAsync();

        TempData["EnrollMessage"] = $"Enrolled in {course.Code}.";
        return RedirectToAction("Index", "StudentDashboard");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Drop(int courseId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        var student = await _db.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (student == null) return RedirectToAction("Index", "StudentDashboard");

        var term = GetCurrentTerm();
        var enrollment = await _db.Enrollments
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.StudentId == student.Id && e.CourseId == courseId && e.Term == term && e.Status == EnrollmentStatus.Approved && !e.Numeric_Grade.HasValue);
        if (enrollment == null)
        {
            TempData["EnrollMessage"] = "Unable to drop: enrollment not found or already graded.";
            return RedirectToAction("Index", "StudentDashboard");
        }

        enrollment.Status = EnrollmentStatus.Dropped;
        enrollment.DroppedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["EnrollMessage"] = $"Dropped {enrollment.Course?.Code ?? enrollment.CourseId.ToString()} successfully.";
        return RedirectToAction("Index", "StudentDashboard");
    }

    private static string GetCurrentTerm()
    {
        var now = DateTime.UtcNow;
        var season = now.Month switch
        {
            >= 1 and <= 4 => "Spring",
            >= 5 and <= 7 => "Summer",
            >= 8 and <= 12 => "Fall",
            _ => "Fall"
        };
        return $"{season} {now.Year}";
    }
}