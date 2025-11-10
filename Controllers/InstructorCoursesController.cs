using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduvisionMvc.Data;
using EduvisionMvc.Models;

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

    public async Task<IActionResult> Details(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.InstructorId == null) return Forbid();

        var course = await _context.Courses
            .Include(c => c.Department)
            .Include(c => c.CourseInstructors).ThenInclude(ci => ci.Instructor)
            .Include(c => c.Materials)
            .Include(c => c.Assignments)
            .Include(c => c.Announcements)
            .Include(c => c.Enrollments).ThenInclude(e => e.Student)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null) return NotFound();

        if (!course.CourseInstructors.Any(ci => ci.InstructorId == user.InstructorId))
            return Forbid();

        return View(course);
    }
}
