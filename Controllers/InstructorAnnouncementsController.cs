using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EduvisionMvc.Data;
using EduvisionMvc.Models;

namespace EduvisionMvc.Controllers;

[Authorize(Roles = "Instructor")]
public class InstructorAnnouncementsController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public InstructorAnnouncementsController(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: InstructorAnnouncements
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.InstructorId == null) return Forbid();

        var announcements = await _context.Announcements
            .Include(a => a.Course)
            .Where(a => a.Course!.CourseInstructors.Any(ci => ci.InstructorId == user.InstructorId))
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return View(announcements);
    }

    // GET: InstructorAnnouncements/Create
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

    // POST: InstructorAnnouncements/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Title,Content,CourseId")] CourseAnnouncement announcement)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.InstructorId == null) return Forbid();

        // Verify instructor teaches this course
        var canAccess = await _context.Courses
            .AnyAsync(c => c.Id == announcement.CourseId && 
                          c.CourseInstructors.Any(ci => ci.InstructorId == user.InstructorId));

        if (!canAccess)
        {
            ModelState.AddModelError("", "You can only create announcements for your own courses.");
            var courses = await _context.Courses
                .Where(c => c.CourseInstructors.Any(ci => ci.InstructorId == user.InstructorId))
                .ToListAsync();
            ViewBag.Courses = new SelectList(courses, "Id", "Code");
            return View(announcement);
        }

        announcement.AuthorId = user.Id;
        announcement.CreatedAt = DateTime.UtcNow;

        _context.Add(announcement);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: InstructorAnnouncements/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user?.InstructorId == null) return Forbid();

        var announcement = await _context.Announcements
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (announcement == null) return NotFound();

        // Verify ownership
        if (announcement.AuthorId != user.Id) return Forbid();

        var courses = await _context.Courses
            .Where(c => c.CourseInstructors.Any(ci => ci.InstructorId == user.InstructorId))
            .ToListAsync();
        ViewBag.Courses = new SelectList(courses, "Id", "Code", announcement.CourseId);

        return View(announcement);
    }

    // POST: InstructorAnnouncements/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Content,CourseId")] CourseAnnouncement announcement)
    {
        if (id != announcement.Id) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user?.InstructorId == null) return Forbid();

        var existing = await _context.Announcements.FindAsync(id);
        if (existing == null) return NotFound();

        // Verify ownership
        if (existing.AuthorId != user.Id) return Forbid();

        existing.Title = announcement.Title;
        existing.Content = announcement.Content;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: InstructorAnnouncements/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user?.InstructorId == null) return Forbid();

        var announcement = await _context.Announcements
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (announcement == null) return NotFound();

        // Verify ownership
        if (announcement.AuthorId != user.Id) return Forbid();

        return View(announcement);
    }

    // POST: InstructorAnnouncements/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.InstructorId == null) return Forbid();

        var announcement = await _context.Announcements.FindAsync(id);
        if (announcement == null) return NotFound();

        // Verify ownership
        if (announcement.AuthorId != user.Id) return Forbid();

        _context.Announcements.Remove(announcement);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
