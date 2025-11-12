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

        // Get enrolled course IDs (no need to check for Dropped since they're deleted)
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
        Console.WriteLine($"[ENROLL] Starting enrollment for courseId: {courseId}");
        
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            Console.WriteLine("[ENROLL] User not found - returning Challenge");
            return Challenge();
        }
        
        var student = await _db.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
        if (student == null)
        {
            Console.WriteLine("[ENROLL] Student profile not found");
            return RedirectToAction("Index", "StudentDashboard");
        }
        
        Console.WriteLine($"[ENROLL] Student found: {student.Name} (ID: {student.Id})");

        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
        if (course == null)
        {
            Console.WriteLine("[ENROLL] Course not found");
            return NotFound();
        }
        
        Console.WriteLine($"[ENROLL] Course found: {course.Code} - {course.Title}");

        var term = GetCurrentTerm();
        Console.WriteLine($"[ENROLL] Current term: {term}");

        // Check if enrollment exists for this student, course, and term
        var existingEnrollment = await _db.Enrollments.FirstOrDefaultAsync(e => 
            e.StudentId == student.Id 
            && e.CourseId == courseId 
            && e.Term == term);

        if (existingEnrollment != null)
        {
            Console.WriteLine("[ENROLL] Already enrolled");
            TempData["EnrollMessage"] = $"Already enrolled in {course.Code} for {term}.";
            return RedirectToAction("Index");
        }

        // Capacity check for new enrollment
        var currentCount = await _db.Enrollments.CountAsync(e => e.CourseId == courseId && e.Term == term && (e.Status == EnrollmentStatus.Approved || e.Status == EnrollmentStatus.Completed));
        Console.WriteLine($"[ENROLL] Current enrollment count: {currentCount}, Capacity: {course.Capacity}");
        if (currentCount >= course.Capacity)
        {
            Console.WriteLine("[ENROLL] Course is full");
            TempData["EnrollMessage"] = $"Course {course.Code} is full.";
            return RedirectToAction("Index");
        }

        // Determine initial status based on whether course requires approval
        var initialStatus = course.RequiresApproval ? EnrollmentStatus.Pending : EnrollmentStatus.Approved;
        Console.WriteLine($"[ENROLL] Course RequiresApproval: {course.RequiresApproval}, Initial Status: {initialStatus}");

        // Create new enrollment
        var newEnrollment = new Enrollment
        {
            StudentId = student.Id,
            CourseId = courseId,
            Term = term,
            Status = initialStatus,
            NumericGrade = null,
            EnrolledDate = DateTime.UtcNow
        };
        
        _db.Enrollments.Add(newEnrollment);
        await _db.SaveChangesAsync();
        
        Console.WriteLine($"[ENROLL] SUCCESS - Enrolled student {student.Id} in course {courseId}");

        var message = course.RequiresApproval 
            ? $"Enrollment in {course.Code} is pending instructor approval."
            : $"Successfully enrolled in {course.Code}!";
        TempData["EnrollMessage"] = message;
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
            .FirstOrDefaultAsync(e => e.StudentId == student.Id && e.CourseId == courseId && e.Term == term && e.Status == EnrollmentStatus.Approved && !e.NumericGrade.HasValue);
        if (enrollment == null)
        {
            TempData["EnrollMessage"] = "Unable to drop: enrollment not found or already graded.";
            return RedirectToAction("Index", "StudentDashboard");
        }

        // Delete the enrollment instead of marking as dropped
        _db.Enrollments.Remove(enrollment);
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