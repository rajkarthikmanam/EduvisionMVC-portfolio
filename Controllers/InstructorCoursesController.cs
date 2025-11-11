using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduvisionMvc.Data;
using EduvisionMvc.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EduvisionMvc.Controllers;

[Authorize(Roles = "Instructor")]
public class InstructorCoursesController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public InstructorCoursesController(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.InstructorId == null)
        {
            return Forbid();
        }

        var courses = await _context.Courses
            .Include(c => c.Department)
            .Include(c => c.CourseInstructors)
            .Where(c => c.CourseInstructors.Any(ci => ci.InstructorId == user.InstructorId))
            .OrderBy(c => c.Code)
            .ToListAsync();

        return View(courses);
    }

    public async Task<IActionResult> Create()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.InstructorId == null) return Forbid();

        var instructor = await _context.Instructors.FindAsync(user.InstructorId);
        if (instructor == null) return NotFound();

        ViewBag.Departments = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name", instructor.DepartmentId);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Course course)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.InstructorId == null) return Forbid();

        var instructor = await _context.Instructors.FindAsync(user.InstructorId);
        if (instructor == null) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.Departments = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name", course.DepartmentId);
            return View(course);
        }

        // Set defaults if not provided
        if (course.Capacity <= 0) course.Capacity = 30;
        if (string.IsNullOrWhiteSpace(course.Level)) course.Level = "Introductory";
        if (string.IsNullOrWhiteSpace(course.DeliveryMode)) course.DeliveryMode = "In-Person";
        if (!course.StartDate.HasValue) course.StartDate = DateTime.UtcNow.Date;
        if (!course.EndDate.HasValue) course.EndDate = DateTime.UtcNow.Date.AddMonths(4);

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        _context.CourseInstructors.Add(new CourseInstructor
        {
            CourseId = course.Id,
            InstructorId = user.InstructorId.Value
        });
        await _context.SaveChangesAsync();

        TempData["Message"] = $"Course {course.Code} created successfully!";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.InstructorId == null) return Forbid();

        var course = await _context.Courses
            .Include(c => c.Department)
            .Include(c => c.CourseInstructors)
                .ThenInclude(ci => ci.Instructor)
            .Include(c => c.Enrollments)
                .ThenInclude(e => e.Student)
            .Include(c => c.Materials)
            .Include(c => c.Assignments)
            .Include(c => c.Announcements)
            .FirstOrDefaultAsync(c => c.Id == id && c.CourseInstructors.Any(ci => ci.InstructorId == user.InstructorId));

        if (course == null) return NotFound();

        ViewBag.CurrentEnrollments = course.Enrollments.Count(e => e.Status == EnrollmentStatus.Approved);
        ViewBag.AverageGrade = course.Enrollments
            .Where(e => e.Numeric_Grade.HasValue)
            .Select(e => e.Numeric_Grade!.Value)
            .DefaultIfEmpty(0)
            .Average();

        return View(course);
    }
}
