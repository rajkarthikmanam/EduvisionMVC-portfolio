using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EduvisionMvc.Data;
using EduvisionMvc.Models;
using Microsoft.AspNetCore.Authorization;

namespace EduvisionMvc.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StudentsController : Controller
    {
        private readonly AppDbContext _context;

        public StudentsController(AppDbContext context)
        {
            _context = context;
        }

        private void PopulateDepartmentsSelectList(object? selected = null)
        {
            ViewBag.Departments = new SelectList(
                _context.Departments.OrderBy(d => d.Name),
                "Id", "Name", selected);
        }

        private void PopulateAdvisorsSelectList(object? selected = null)
        {
            ViewBag.Advisors = new SelectList(
                _context.Instructors.OrderBy(i => i.LastName).Select(i => new { i.Id, FullName = i.FirstName + " " + i.LastName }),
                "Id", "FullName", selected);
        }

        // GET: Students
        public async Task<IActionResult> Index()
        {
            return View(await _context.Students.Include(s => s.Department).Include(s => s.AdvisorInstructor).ToListAsync());
        }

        // GET: Students/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.Department)
                .Include(s => s.AdvisorInstructor)
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // GET: Students/Create
        public IActionResult Create()
        {
            PopulateDepartmentsSelectList();
            PopulateAdvisorsSelectList();
            
            // Set default values
            var student = new Student
            {
                TotalCredits = 120, // Credits required for graduation
                EnrollmentDate = DateTime.UtcNow
            };
            
            return View(student);
        }

        // POST: Students/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Email,Major,Age,Gpa,DepartmentId,AdvisorInstructorId,Phone,AcademicLevel,TotalCredits,EnrollmentDate")] Student student)
        {
            if (ModelState.IsValid)
            {
                // Auto-assign advisor from department if not selected
                if (!student.AdvisorInstructorId.HasValue && student.DepartmentId.HasValue)
                {
                    var advisor = await _context.Instructors
                        .Where(i => i.DepartmentId == student.DepartmentId.Value)
                        .OrderBy(i => i.LastName)
                        .FirstOrDefaultAsync();
                    
                    if (advisor != null)
                    {
                        student.AdvisorInstructorId = advisor.Id;
                    }
                }
                
                try
                {
                    _context.Add(student);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Student created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true || 
                                                   ex.InnerException?.Message.Contains("duplicate") == true)
                {
                    TempData["Error"] = "Duplicate detected: A student with this email already exists.";
                }
                catch (DbUpdateException ex)
                {
                    TempData["Error"] = $"Error creating student: {ex.InnerException?.Message ?? ex.Message}";
                }
            }
            PopulateDepartmentsSelectList(student.DepartmentId);
            PopulateAdvisorsSelectList(student.AdvisorInstructorId);
            return View(student);
        }

        // GET: Students/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            PopulateDepartmentsSelectList(student.DepartmentId);
            PopulateAdvisorsSelectList(student.AdvisorInstructorId);
            return View(student);
        }

        // POST: Students/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Email,Major,Age,Gpa,DepartmentId,AdvisorInstructorId,Phone,AcademicLevel,TotalCredits,EnrollmentDate")] Student student)
        {
            if (id != student.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Recalculate GPA based on enrollments
                    var enrollments = await _context.Enrollments
                        .Include(e => e.Course)
                        .Where(e => e.StudentId == student.Id && e.NumericGrade.HasValue && e.Status != EnrollmentStatus.Dropped)
                        .ToListAsync();
                    
                    if (enrollments.Any())
                    {
                        student.Gpa = enrollments.Average(e => e.NumericGrade!.Value);
                    }
                    
                    // TotalCredits field represents credits REQUIRED (not completed)
                    // Ensure it's at least 6
                    if (student.TotalCredits < 6)
                    {
                        student.TotalCredits = 120; // Default required credits
                    }
                    
                    _context.Update(student);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Student updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true || 
                                                   ex.InnerException?.Message.Contains("duplicate") == true)
                {
                    TempData["Error"] = "Duplicate detected: A student with this email already exists.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (DbUpdateException ex)
                {
                    TempData["Error"] = $"Error updating student: {ex.InnerException?.Message ?? ex.Message}";
                }
            }
            PopulateDepartmentsSelectList(student.DepartmentId);
            PopulateAdvisorsSelectList(student.AdvisorInstructorId);
            return View(student);
        }

        // GET: Students/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.Department)
                .Include(s => s.AdvisorInstructor)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Course)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);
                
            if (student == null)
            {
                TempData["Error"] = "Student not found.";
                return RedirectToAction(nameof(Index));
            }

            // Check for truly active enrollments (no grade AND course hasn't ended yet)
            var activeEnrollments = student.Enrollments
                .Where(e => !e.NumericGrade.HasValue && 
                           (e.Course == null || e.Course.EndDate == null || e.Course.EndDate > DateTime.UtcNow))
                .ToList();
            
            if (activeEnrollments.Any())
            {
                TempData["Error"] = $"Cannot delete student '{student.Name}' because they have {activeEnrollments.Count} active enrollment(s) in courses that haven't ended yet.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Delete associated user account if exists
                if (student.User != null)
                {
                    _context.Users.Remove(student.User);
                }
                
                // Delete all enrollments
                if (student.Enrollments.Any())
                {
                    _context.Enrollments.RemoveRange(student.Enrollments);
                }
                
                // Delete the student
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = $"Student '{student.Name}' deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting student: {ex.InnerException?.Message ?? ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.Id == id);
        }
    }
}
