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
    public class CoursesController : Controller
    {
        private readonly AppDbContext _context;

        public CoursesController(AppDbContext context)
        {
            _context = context;
        }

        private void PopulateDepartmentsSelectList(object? selected = null)
        {
            ViewBag.Departments = new SelectList(
                _context.Departments.OrderBy(d => d.Name),
                "Id", "Name", selected);
        }

        private void PopulateLevelsSelectList(object? selected = null)
        {
            var levels = new List<string> { "Introductory", "Intermediate", "Advanced", "Graduate" };
            ViewBag.Levels = new SelectList(levels, selected);
        }

        private void PopulateDeliveryModesSelectList(object? selected = null)
        {
            var modes = new List<string> { "Online", "In-Person", "Hybrid" };
            ViewBag.DeliveryModes = new SelectList(modes, selected);
        }

        private void PopulateInstructorsSelectList(int? selectedInstructorId = null)
        {
            var instructors = _context.Instructors
                .OrderBy(i => i.LastName)
                .ThenBy(i => i.FirstName)
                .Select(i => new { i.Id, DisplayName = i.FirstName + " " + i.LastName })
                .ToList();

            ViewBag.Instructors = new SelectList(instructors, "Id", "DisplayName", selectedInstructorId);
        }

        // GET: Courses
        public async Task<IActionResult> Index()
{
    var courses = await _context.Courses
        .Include(c => c.Department)
        .Include(c => c.CourseInstructors)
            .ThenInclude(ci => ci.Instructor)
        .ToListAsync();
    return View(courses);
}


        // GET: Courses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.Department)
                .Include(c => c.CourseInstructors)
                    .ThenInclude(ci => ci.Instructor)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (course == null) return NotFound();

            return View(course);
        }

        // GET: Courses/Create
        public IActionResult Create()
        {
            PopulateDepartmentsSelectList();
            PopulateLevelsSelectList();
            PopulateDeliveryModesSelectList();
            PopulateInstructorsSelectList();
            return View();
        }

        // POST: Courses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Code,Title,Description,Credits,Capacity,RequiresApproval,Level,DeliveryMode,Prerequisites,DepartmentId,Schedule,StartDate,EndDate,Location")] Course course, int? instructorId)
        {
            // Remove validation errors for navigation properties
            ModelState.Remove("Department");
            
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(course);
                    await _context.SaveChangesAsync();

                    // Add instructor assignment if provided
                    if (instructorId.HasValue)
                    {
                        _context.CourseInstructors.Add(new CourseInstructor
                        {
                            CourseId = course.Id,
                            InstructorId = instructorId.Value
                        });
                        await _context.SaveChangesAsync();
                    }
                    
                    TempData["Success"] = "Course created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true || 
                                                   ex.InnerException?.Message.Contains("duplicate") == true)
                {
                    TempData["Error"] = "Duplicate detected: A course with this code already exists.";
                }
                catch (DbUpdateException ex)
                {
                    TempData["Error"] = $"Error creating course: {ex.InnerException?.Message ?? ex.Message}";
                }
            }
            PopulateDepartmentsSelectList(course.DepartmentId);
            PopulateLevelsSelectList(course.Level);
            PopulateDeliveryModesSelectList(course.DeliveryMode);
            PopulateInstructorsSelectList();
            return View(course);
        }

        // GET: Courses/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            PopulateDepartmentsSelectList(course.DepartmentId);
            PopulateLevelsSelectList(course.Level);
            PopulateDeliveryModesSelectList(course.DeliveryMode);
            var selectedInstructorId = await _context.CourseInstructors
                .Where(ci => ci.CourseId == course.Id)
                .Select(ci => ci.InstructorId)
                .FirstOrDefaultAsync();
            PopulateInstructorsSelectList(selectedInstructorId == 0 ? null : selectedInstructorId);
            return View(course);
        }

        // POST: Courses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Code,Title,Description,Credits,Capacity,RequiresApproval,Level,DeliveryMode,Prerequisites,DepartmentId,Schedule,StartDate,EndDate,Location")] Course course, int? instructorId)
        {
            if (id != course.Id) return NotFound();

            // Remove validation errors for navigation properties
            ModelState.Remove("Department");

            if (ModelState.IsValid)
            {
                try
                {
                    // Track if RequiresApproval changed from true to false
                    var originalCourse = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
                    var requiresApprovalChanged = originalCourse != null && 
                                                  originalCourse.RequiresApproval && 
                                                  !course.RequiresApproval;

                    _context.Update(course);

                    // If RequiresApproval changed from true to false, auto-approve all pending enrollments
                    if (requiresApprovalChanged)
                    {
                        var pendingEnrollments = await _context.Enrollments
                            .Where(e => e.CourseId == id && e.Status == EnrollmentStatus.Pending)
                            .ToListAsync();

                        foreach (var enrollment in pendingEnrollments)
                        {
                            enrollment.Status = EnrollmentStatus.Approved;
                            enrollment.ApprovedDate = DateTime.UtcNow;
                        }
                    }

                    // Update instructor assignment (single)
                    var existingAssignments = await _context.CourseInstructors
                        .Where(ci => ci.CourseId == id)
                        .ToListAsync();

                    _context.CourseInstructors.RemoveRange(existingAssignments);

                    if (instructorId.HasValue)
                    {
                        _context.CourseInstructors.Add(new CourseInstructor
                        {
                            CourseId = course.Id,
                            InstructorId = instructorId.Value
                        });
                    }

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Course updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true || 
                                                   ex.InnerException?.Message.Contains("duplicate") == true)
                {
                    TempData["Error"] = "Duplicate detected: A course with this code already exists.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Courses.Any(e => e.Id == course.Id))
                        return NotFound();
                    else throw;
                }
                catch (DbUpdateException ex)
                {
                    TempData["Error"] = $"Error updating course: {ex.InnerException?.Message ?? ex.Message}";
                }
            }
            PopulateDepartmentsSelectList(course.DepartmentId);
            PopulateLevelsSelectList(course.Level);
            PopulateDeliveryModesSelectList(course.DeliveryMode);
            var selectedInstructorId2 = await _context.CourseInstructors
                .Where(ci => ci.CourseId == course.Id)
                .Select(ci => ci.InstructorId)
                .FirstOrDefaultAsync();
            PopulateInstructorsSelectList(selectedInstructorId2 == 0 ? null : selectedInstructorId2);
            return View(course);
        }

        // GET: Courses/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.Department)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (course == null) return NotFound();

            return View(course);
        }

        // POST: Courses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course != null) _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
