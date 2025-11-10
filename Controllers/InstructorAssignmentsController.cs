using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EduvisionMvc.Data;
using EduvisionMvc.Models;

namespace EduvisionMvc.Controllers;

[Authorize(Roles = "Instructor")]
public class InstructorAssignmentsController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public InstructorAssignmentsController(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: InstructorAssignments
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.InstructorId == null) return Forbid();

        var assignments = await _context.Assignments
            .Include(a => a.Course)
            .Include(a => a.Submissions)
            .Where(a => a.Course!.CourseInstructors.Any(ci => ci.InstructorId == user.InstructorId))
            .OrderByDescending(a => a.CreatedDate)
            .ToListAsync();

        return View(assignments);
    }

    // GET: InstructorAssignments/Create
    public async Task<IActionResult> Create()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.InstructorId == null) return Forbid();

        var courses = await _context.Courses
            .Where(c => c.CourseInstructors.Any(ci => ci.InstructorId == user.InstructorId))
            .OrderBy(c => c.Code)
            .ToListAsync();

        ViewBag.Courses = new SelectList(courses, "Id", "Code");
        return View();
    }

    // POST: InstructorAssignments/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Title,Description,Instructions,DueDate,MaxPoints,CourseId,IsPublished")] Assignment assignment)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.InstructorId == null) return Forbid();

        // Verify instructor teaches this course
        var canAccess = await _context.Courses
            .AnyAsync(c => c.Id == assignment.CourseId && 
                          c.CourseInstructors.Any(ci => ci.InstructorId == user.InstructorId));

        if (!canAccess)
        {
            ModelState.AddModelError("", "You can only create assignments for your own courses.");
            var courses = await _context.Courses
                .Where(c => c.CourseInstructors.Any(ci => ci.InstructorId == user.InstructorId))
                .ToListAsync();
            ViewBag.Courses = new SelectList(courses, "Id", "Code");
            return View(assignment);
        }

        assignment.CreatedDate = DateTime.UtcNow;
        _context.Add(assignment);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: InstructorAssignments/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user?.InstructorId == null) return Forbid();

        var assignment = await _context.Assignments
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (assignment == null) return NotFound();

        // Verify ownership
        var canAccess = await _context.Courses
            .AnyAsync(c => c.Id == assignment.CourseId && 
                          c.CourseInstructors.Any(ci => ci.InstructorId == user.InstructorId));

        if (!canAccess) return Forbid();

        var courses = await _context.Courses
            .Where(c => c.CourseInstructors.Any(ci => ci.InstructorId == user.InstructorId))
            .ToListAsync();
        ViewBag.Courses = new SelectList(courses, "Id", "Code", assignment.CourseId);

        return View(assignment);
    }

    // POST: InstructorAssignments/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Instructions,DueDate,MaxPoints,CourseId,IsPublished")] Assignment assignment)
    {
        if (id != assignment.Id) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user?.InstructorId == null) return Forbid();

        var existing = await _context.Assignments.FindAsync(id);
        if (existing == null) return NotFound();

        // Verify ownership
        var canAccess = await _context.Courses
            .AnyAsync(c => c.Id == existing.CourseId && 
                          c.CourseInstructors.Any(ci => ci.InstructorId == user.InstructorId));

        if (!canAccess) return Forbid();

        existing.Title = assignment.Title;
        existing.Description = assignment.Description;
        existing.Instructions = assignment.Instructions;
        existing.DueDate = assignment.DueDate;
        existing.MaxPoints = assignment.MaxPoints;
        existing.IsPublished = assignment.IsPublished;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: InstructorAssignments/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user?.InstructorId == null) return Forbid();

        var assignment = await _context.Assignments
            .Include(a => a.Course)
            .Include(a => a.Submissions)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (assignment == null) return NotFound();

        // Verify ownership
        var canAccess = await _context.Courses
            .AnyAsync(c => c.Id == assignment.CourseId && 
                          c.CourseInstructors.Any(ci => ci.InstructorId == user.InstructorId));

        if (!canAccess) return Forbid();

        return View(assignment);
    }

    // POST: InstructorAssignments/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.InstructorId == null) return Forbid();

        var assignment = await _context.Assignments.FindAsync(id);
        if (assignment == null) return NotFound();

        // Verify ownership
        var canAccess = await _context.Courses
            .AnyAsync(c => c.Id == assignment.CourseId && 
                          c.CourseInstructors.Any(ci => ci.InstructorId == user.InstructorId));

        if (!canAccess) return Forbid();

        _context.Assignments.Remove(assignment);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
