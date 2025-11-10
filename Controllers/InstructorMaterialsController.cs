using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EduvisionMvc.Data;
using EduvisionMvc.Models;

namespace EduvisionMvc.Controllers;

[Authorize(Roles = "Instructor")]
public class InstructorMaterialsController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public InstructorMaterialsController(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: InstructorMaterials
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.InstructorId == null) return Forbid();

        var materials = await _context.CourseMaterials
            .Include(m => m.Course)
            .Where(m => m.Course.CourseInstructors.Any(ci => ci.InstructorId == user.InstructorId))
            .OrderByDescending(m => m.UploadedDate)
            .ToListAsync();

        return View(materials);
    }

    // GET: InstructorMaterials/Create
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

    // POST: InstructorMaterials/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Title,Description,Url,Type,CourseId,IsPublished")] CourseMaterial material)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.InstructorId == null) return Forbid();

        // Verify instructor teaches this course
        var canAccess = await _context.Courses
            .AnyAsync(c => c.Id == material.CourseId && 
                          c.CourseInstructors.Any(ci => ci.InstructorId == user.InstructorId));

        if (!canAccess)
        {
            ModelState.AddModelError("", "You can only add materials to your own courses.");
            var courses = await _context.Courses
                .Where(c => c.CourseInstructors.Any(ci => ci.InstructorId == user.InstructorId))
                .ToListAsync();
            ViewBag.Courses = new SelectList(courses, "Id", "Code");
            return View(material);
        }

        material.UploadedById = user.Id;
        material.UploadedDate = DateTime.UtcNow;

        _context.Add(material);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: InstructorMaterials/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user?.InstructorId == null) return Forbid();

        var material = await _context.CourseMaterials
            .Include(m => m.Course)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (material == null) return NotFound();

        // Verify ownership
        var canAccess = await _context.Courses
            .AnyAsync(c => c.Id == material.CourseId && 
                          c.CourseInstructors.Any(ci => ci.InstructorId == user.InstructorId));

        if (!canAccess) return Forbid();

        var courses = await _context.Courses
            .Where(c => c.CourseInstructors.Any(ci => ci.InstructorId == user.InstructorId))
            .ToListAsync();
        ViewBag.Courses = new SelectList(courses, "Id", "Code", material.CourseId);

        return View(material);
    }

    // POST: InstructorMaterials/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Url,Type,CourseId,IsPublished")] CourseMaterial material)
    {
        if (id != material.Id) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user?.InstructorId == null) return Forbid();

        var existing = await _context.CourseMaterials.FindAsync(id);
        if (existing == null) return NotFound();

        // Verify ownership
        var canAccess = await _context.Courses
            .AnyAsync(c => c.Id == existing.CourseId && 
                          c.CourseInstructors.Any(ci => ci.InstructorId == user.InstructorId));

        if (!canAccess) return Forbid();

        existing.Title = material.Title;
        existing.Description = material.Description;
        existing.Url = material.Url;
        existing.Type = material.Type;
        existing.IsPublished = material.IsPublished;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: InstructorMaterials/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user?.InstructorId == null) return Forbid();

        var material = await _context.CourseMaterials
            .Include(m => m.Course)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (material == null) return NotFound();

        // Verify ownership
        var canAccess = await _context.Courses
            .AnyAsync(c => c.Id == material.CourseId && 
                          c.CourseInstructors.Any(ci => ci.InstructorId == user.InstructorId));

        if (!canAccess) return Forbid();

        return View(material);
    }

    // POST: InstructorMaterials/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.InstructorId == null) return Forbid();

        var material = await _context.CourseMaterials.FindAsync(id);
        if (material == null) return NotFound();

        // Verify ownership
        var canAccess = await _context.Courses
            .AnyAsync(c => c.Id == material.CourseId && 
                          c.CourseInstructors.Any(ci => ci.InstructorId == user.InstructorId));

        if (!canAccess) return Forbid();

        _context.CourseMaterials.Remove(material);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
