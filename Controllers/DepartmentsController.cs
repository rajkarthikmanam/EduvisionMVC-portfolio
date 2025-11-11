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
    public class DepartmentsController : Controller
    {
        private readonly AppDbContext _context;

        public DepartmentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Departments
        public async Task<IActionResult> Index()
        {
            return View(await _context.Departments.OrderBy(d => d.Code).ToListAsync());
        }

        // GET: Departments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments
                .Include(d => d.Chair)
                .Include(d => d.Courses)
                .Include(d => d.Students)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }

        // GET: Departments/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Departments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Code,Name,Description,OfficeLocation,Phone,Email,Website")] Department department)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(department);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Department created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true || 
                                                   ex.InnerException?.Message.Contains("duplicate") == true)
                {
                    TempData["Error"] = "Duplicate detected: A department with this code or name already exists.";
                }
                catch (DbUpdateException ex)
                {
                    TempData["Error"] = $"Error creating department: {ex.InnerException?.Message ?? ex.Message}";
                }
            }
            return View(department);
        }

        // GET: Departments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                return NotFound();
            }
            return View(department);
        }

        // POST: Departments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Code,Name,Description,OfficeLocation,Phone,Email,Website")] Department department)
        {
            if (id != department.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(department);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Department updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true || 
                                                   ex.InnerException?.Message.Contains("duplicate") == true)
                {
                    TempData["Error"] = "Duplicate detected: A department with this code or name already exists.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepartmentExists(department.Id))
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
                    TempData["Error"] = $"Error updating department: {ex.InnerException?.Message ?? ex.Message}";
                }
            }
            return View(department);
        }

        // GET: Departments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments
                .FirstOrDefaultAsync(m => m.Id == id);
            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }

        // POST: Departments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var department = await _context.Departments
                .Include(d => d.Courses)
                .Include(d => d.Students)
                .FirstOrDefaultAsync(d => d.Id == id);
            
            if (department == null)
            {
                TempData["Error"] = "Department not found.";
                return RedirectToAction(nameof(Index));
            }

            // Check if department has courses
            if (department.Courses.Any())
            {
                TempData["Error"] = $"Cannot delete department '{department.Name}' because it has {department.Courses.Count} course(s). Please reassign or delete courses first.";
                return RedirectToAction(nameof(Index));
            }

            // Check if department has students
            if (department.Students.Any())
            {
                TempData["Error"] = $"Cannot delete department '{department.Name}' because it has {department.Students.Count} student(s). Please reassign or delete students first.";
                return RedirectToAction(nameof(Index));
            }

            // Check if any instructor belongs to this department
            var instructorsCount = await _context.Instructors.CountAsync(i => i.DepartmentId == id);
            if (instructorsCount > 0)
            {
                TempData["Error"] = $"Cannot delete department '{department.Name}' because it has {instructorsCount} instructor(s). Please reassign or delete instructors first.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Department '{department.Name}' deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting department: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool DepartmentExists(int id)
        {
            return _context.Departments.Any(e => e.Id == id);
        }
    }
}
