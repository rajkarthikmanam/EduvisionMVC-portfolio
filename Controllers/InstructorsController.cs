using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduvisionMvc.Data;
using EduvisionMvc.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;

namespace EduvisionMvc.Controllers
{
    [Authorize(Roles = "Admin")]
    public class InstructorsController : Controller
    {
        private readonly AppDbContext _context;

        public InstructorsController(AppDbContext context)
        {
            _context = context;
        }

        private void PopulateDepartmentsSelectList(object? selected = null)
        {
            ViewBag.Departments = new SelectList(
                _context.Departments.OrderBy(d => d.Name),
                "Id", "Name", selected);
        }

        // GET: Instructors
        public async Task<IActionResult> Index()
        {
            var q = _context.Instructors
                .Include(i => i.Department)
                .AsNoTracking();
            return View(await q.ToListAsync());
        }

        // GET: Instructors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var instructor = await _context.Instructors
                .Include(i => i.Department)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (instructor == null) return NotFound();

            return View(instructor);
        }

        // GET: Instructors/Create
        public IActionResult Create()
        {
            PopulateDepartmentsSelectList();
            return View();
        }

        // POST: Instructors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,FirstName,LastName,Email,DepartmentId,Title,OfficeLocation,Phone,OfficeHours,HireDate,Bio")] Instructor instructor)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(instructor);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Instructor created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true || 
                                                   ex.InnerException?.Message.Contains("duplicate") == true)
                {
                    TempData["Error"] = "Duplicate detected: An instructor with this email already exists.";
                }
                catch (DbUpdateException ex)
                {
                    TempData["Error"] = $"Error creating instructor: {ex.InnerException?.Message ?? ex.Message}";
                }
            }
            PopulateDepartmentsSelectList(instructor.DepartmentId);
            return View(instructor);
        }

        // GET: Instructors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var instructor = await _context.Instructors.FindAsync(id);
            if (instructor == null) return NotFound();

            PopulateDepartmentsSelectList(instructor.DepartmentId);
            return View(instructor);
        }

        // POST: Instructors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,Email,DepartmentId,Title,OfficeLocation,Phone,OfficeHours,HireDate,Bio")] Instructor instructor)
        {
            if (id != instructor.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(instructor);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Instructor updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true || 
                                                   ex.InnerException?.Message.Contains("duplicate") == true)
                {
                    TempData["Error"] = "Duplicate detected: An instructor with this email already exists.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Instructors.Any(e => e.Id == instructor.Id))
                        return NotFound();
                    else throw;
                }
                catch (DbUpdateException ex)
                {
                    TempData["Error"] = $"Error updating instructor: {ex.InnerException?.Message ?? ex.Message}";
                }
            }
            PopulateDepartmentsSelectList(instructor.DepartmentId);
            return View(instructor);
        }

        // GET: Instructors/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var instructor = await _context.Instructors
                .Include(i => i.Department)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (instructor == null) return NotFound();

            return View(instructor);
        }

        // POST: Instructors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var instructor = await _context.Instructors
                .Include(i => i.CourseInstructors)
                .FirstOrDefaultAsync(i => i.Id == id);
                
            if (instructor == null)
            {
                return NotFound();
            }

            // Check for course assignments
            if (instructor.CourseInstructors.Any())
            {
                TempData["Error"] = $"Cannot delete instructor '{instructor.FirstName} {instructor.LastName}' because they are assigned to {instructor.CourseInstructors.Count} course(s). Please remove course assignments first.";
                return RedirectToAction(nameof(Index));
            }

            // Check if instructor is a department chair
            var isDepartmentChair = await _context.Departments.AnyAsync(d => d.ChairId == id);
            if (isDepartmentChair)
            {
                TempData["Error"] = $"Cannot delete instructor '{instructor.FirstName} {instructor.LastName}' because they are a department chair. Please assign a new chair first.";
                return RedirectToAction(nameof(Index));
            }

            // Check if instructor is an advisor
            var hasAdvisees = await _context.Students.AnyAsync(s => s.AdvisorInstructorId == id);
            if (hasAdvisees)
            {
                TempData["Error"] = $"Cannot delete instructor '{instructor.FirstName} {instructor.LastName}' because they are advising students. Please reassign advisees first.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Delete associated User account if exists
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == instructor.Email);
                if (user != null)
                {
                    _context.Users.Remove(user);
                }

                _context.Instructors.Remove(instructor);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Instructor '{instructor.FirstName} {instructor.LastName}' deleted successfully!";
            }
            catch (DbUpdateException ex)
            {
                TempData["Error"] = $"Cannot delete instructor. Error: {ex.InnerException?.Message ?? ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
