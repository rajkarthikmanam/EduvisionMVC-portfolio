using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using EduvisionMvc.Data;
using EduvisionMvc.Models;

namespace EduvisionMvc.Controllers;

public class EnrollmentsController : Controller
{
    private readonly AppDbContext _context;
    public EnrollmentsController(AppDbContext context) => _context = context;

    // ---------- LIST ----------
    public async Task<IActionResult> Index()
    {
        var q = _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
                .ThenInclude(c => c.CourseInstructors)
                    .ThenInclude(ci => ci.Instructor);

        var data = await q.OrderBy(e => e.Student.Name)
                          .ThenBy(e => e.Course.Title)
                          .ToListAsync();
        return View(data);
    }

    // ---------- DETAILS ----------
    public async Task<IActionResult> Details(int id)
    {
        var e = await _context.Enrollments
            .Include(x => x.Student)
            .Include(x => x.Course)
                .ThenInclude(c => c.CourseInstructors)
                    .ThenInclude(ci => ci.Instructor)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (e == null) return NotFound();
        return View(e);
    }

    // ---------- CREATE ----------
    [HttpGet]
    public IActionResult Create()
    {
        ViewData["StudentId"] = new SelectList(_context.Students.OrderBy(s => s.Name), "Id", "Name");
        ViewData["CourseId"]  = new SelectList(_context.Courses.OrderBy(c => c.Title), "Id", "Title");
        return View(new Enrollment { Term = "Fall 2025" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,StudentId,CourseId,Term,Numeric_Grade")] Enrollment enrollment)
    {
        if (ModelState.IsValid)
        {
            _context.Add(enrollment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewData["StudentId"] = new SelectList(_context.Students.OrderBy(s => s.Name), "Id", "Name", enrollment.StudentId);
        ViewData["CourseId"]  = new SelectList(_context.Courses.OrderBy(c => c.Title), "Id", "Title", enrollment.CourseId);
        return View(enrollment);
    }

    // ---------- EDIT ----------
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var enrollment = await _context.Enrollments.FindAsync(id);
        if (enrollment == null) return NotFound();

        ViewData["StudentId"] = new SelectList(_context.Students.OrderBy(s => s.Name), "Id", "Name", enrollment.StudentId);
        ViewData["CourseId"]  = new SelectList(_context.Courses.OrderBy(c => c.Title), "Id", "Title", enrollment.CourseId);
        return View(enrollment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,StudentId,CourseId,Term,Numeric_Grade")] Enrollment enrollment)
    {
        if (id != enrollment.Id) return NotFound();

        if (ModelState.IsValid)
        {
            _context.Update(enrollment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        ViewData["StudentId"] = new SelectList(_context.Students.OrderBy(s => s.Name), "Id", "Name", enrollment.StudentId);
        ViewData["CourseId"]  = new SelectList(_context.Courses.OrderBy(c => c.Title), "Id", "Title", enrollment.CourseId);
        return View(enrollment);
    }

    // ---------- DELETE ----------
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await _context.Enrollments
            .Include(x => x.Student)
            .Include(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (e == null) return NotFound();
        return View(e);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var e = await _context.Enrollments.FindAsync(id);
        if (e == null) return NotFound();

        _context.Enrollments.Remove(e);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
