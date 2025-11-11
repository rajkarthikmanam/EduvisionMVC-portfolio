using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using EduvisionMvc.Data;
using EduvisionMvc.Models;
using Microsoft.AspNetCore.Authorization;

namespace EduvisionMvc.Controllers;

[Authorize(Roles = "Admin")]
public class EnrollmentsController : Controller
{
    private readonly AppDbContext _context;
    private readonly IHubContext<EduvisionMvc.Hubs.DashboardHub> _hub;
    public EnrollmentsController(AppDbContext context, IHubContext<EduvisionMvc.Hubs.DashboardHub> hub)
    { _context = context; _hub = hub; }

    // ---------- LIST ----------
    public async Task<IActionResult> Index()
    {
        var q = _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
                .ThenInclude(c => c!.CourseInstructors)
                    .ThenInclude(ci => ci.Instructor);

        var data = await q.OrderBy(e => e.Student == null ? string.Empty : e.Student!.Name)
                  .ThenBy(e => e.Course == null ? string.Empty : e.Course!.Title)
                              .ToListAsync();

        // Auto-update statuses for courses that have ended
        // BUT DO NOT auto-complete enrollments for the current active term (Fall 2025)
        bool hasChanges = false;
        var affectedStudents = new HashSet<int>();
        foreach (var enrollment in data)
        {
            // Skip auto-completion for current term enrollments
            if (IsCurrentTerm(enrollment.Term))
            {
                continue; // Don't auto-complete Fall 2025 enrollments
            }

            if (enrollment.Course?.EndDate != null && enrollment.Course.EndDate < DateTime.UtcNow)
            {
                // Course has ended - mark all non-rejected enrollments as Completed
                if (enrollment.Status != EnrollmentStatus.Rejected && enrollment.Status != EnrollmentStatus.Completed)
                {
                    enrollment.Status = EnrollmentStatus.Completed;
                    // Default grade 3.5 if none
                    if (!enrollment.Numeric_Grade.HasValue)
                    {
                        enrollment.Numeric_Grade = 3.5m;
                        enrollment.UpdateLetterGrade();
                    }
                    enrollment.CompletedDate = enrollment.Course.EndDate;
                    hasChanges = true;
                    affectedStudents.Add(enrollment.StudentId);
                }
            }
        }

        if (hasChanges)
        {
            await _context.SaveChangesAsync();
            // Recalculate GPA for all affected students
            foreach (var sid in affectedStudents)
            {
                await RecalculateGpaAsync(sid);
            }
        }

        return View(data);
    }

    // ---------- DETAILS ----------
    public async Task<IActionResult> Details(int id)
    {
        var e = await _context.Enrollments
            .Include(x => x.Student)
            .Include(x => x.Course)
                .ThenInclude(c => c!.CourseInstructors)
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
    public async Task<IActionResult> Create([Bind("Id,StudentId,CourseId,Term,Numeric_Grade,Status")] Enrollment enrollment)
    {
        // Business rules:
        // Fall 2025: Approved/Pending status, no grade
        // Spring/Summer 2025: Completed status, grade required (default 3.5 if not provided)

        NormalizeIncomingTerm(enrollment);

        if (IsPastTerm(enrollment.Term))
        {
            // Past term: default grade to 3.5 if not provided
            if (!enrollment.Numeric_Grade.HasValue)
            {
                enrollment.Numeric_Grade = 3.5m;
            }
            // Force status to Completed for past terms
            enrollment.Status = EnrollmentStatus.Completed;
        }
        else if (IsCurrentTerm(enrollment.Term))
        {
            // Active term: no grade allowed
            if (enrollment.Numeric_Grade.HasValue)
            {
                ModelState.AddModelError("Numeric_Grade", "Active term (Fall 2025) enrollment cannot have a grade yet.");
            }
            // Status should be Pending or Approved (already set from form)
            if (enrollment.Status != EnrollmentStatus.Pending && enrollment.Status != EnrollmentStatus.Approved)
            {
                enrollment.Status = EnrollmentStatus.Approved; // Default to Approved
            }
        }
        else
        {
            ModelState.AddModelError("Term", "Future terms are not allowed.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                enrollment.EnrolledDate = DateTime.UtcNow;
                
                // For Fall 2025, check if course requires approval to set correct initial status
                if (IsCurrentTerm(enrollment.Term))
                {
                    var course = await _context.Courses.FindAsync(enrollment.CourseId);
                    if (course != null && course.RequiresApproval && enrollment.Status == EnrollmentStatus.Approved)
                    {
                        // If course requires approval, set to Pending initially (instructor must approve later)
                        enrollment.Status = EnrollmentStatus.Pending;
                    }
                }
                
                if (enrollment.Status == EnrollmentStatus.Completed && enrollment.Numeric_Grade.HasValue)
                {
                    enrollment.CompletedDate = DateTime.UtcNow;
                    enrollment.UpdateLetterGrade();
                }

                _context.Add(enrollment);
                await _context.SaveChangesAsync();
                // Ensure student's GPA reflects any graded past-term creation
                await RecalculateGpaAsync(enrollment.StudentId);
                TempData["Success"] = "Enrollment created.";
                await _hub.Clients.All.SendCoreAsync("metricsUpdated", new object[] { new { ts = DateTime.UtcNow } });
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true || 
                                               ex.InnerException?.Message.Contains("duplicate") == true)
            {
                TempData["Error"] = "Duplicate detected: This student is already enrolled in this course for this term.";
            }
            catch (DbUpdateException ex)
            {
                TempData["Error"] = $"Error creating enrollment: {ex.InnerException?.Message ?? ex.Message}";
            }
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
    public async Task<IActionResult> Edit(int id, [Bind("Id,StudentId,CourseId,Term,Numeric_Grade,Status")] Enrollment enrollment)
    {
        if (id != enrollment.Id) return NotFound();

        // Load tracked entity and apply posted changes
        var existing = await _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (existing == null) return NotFound();

        // Clear ModelState to avoid binding issues with computed properties
        ModelState.Clear();

        existing.StudentId = enrollment.StudentId;
        existing.CourseId = enrollment.CourseId;
        existing.Term = (enrollment.Term ?? "").Trim();
        existing.Status = enrollment.Status;
        existing.Numeric_Grade = enrollment.Numeric_Grade;

        NormalizeIncomingTerm(existing);

        // Term-based validation rules:
        // Fall 2025 (active): Only Pending/Approved status allowed, no grade
        // Spring/Summer 2025 (past): Only Completed status allowed, grade required

        if (IsCurrentTerm(existing.Term))
        {
            // Fall 2025: Active term
            if (existing.Status != EnrollmentStatus.Pending && existing.Status != EnrollmentStatus.Approved)
            {
                ModelState.AddModelError("Status", "Fall 2025 (active term) can only have Pending or Approved status.");
            }
            if (existing.Numeric_Grade.HasValue)
            {
                ModelState.AddModelError("Numeric_Grade", "Fall 2025 (active term) cannot have a grade yet. Leave it empty.");
            }
        }
        else if (IsPastTerm(existing.Term))
        {
            // Spring/Summer 2025: Past terms
            if (existing.Status != EnrollmentStatus.Completed)
            {
                ModelState.AddModelError("Status", "Spring/Summer 2025 (past terms) must have Completed status.");
            }
            if (!existing.Numeric_Grade.HasValue)
            {
                ModelState.AddModelError("Numeric_Grade", "Spring/Summer 2025 (past terms) must have a grade.");
            }
        }

        // If grade is present, update letter grade and set completed date
        if (existing.Numeric_Grade.HasValue)
        {
            existing.UpdateLetterGrade();
            if (existing.CompletedDate == null)
            {
                existing.CompletedDate = DateTime.UtcNow;
            }
        }
        else
        {
            // No grade - clear completion data
            existing.CompletedDate = null;
            existing.LetterGrade = null;
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(existing);
                await _context.SaveChangesAsync();

                // GPA recalculation only if numeric grade present (past term)
                await RecalculateGpaAsync(existing.StudentId);
                TempData["Success"] = "Enrollment updated successfully.";
                await _hub.Clients.All.SendCoreAsync("metricsUpdated", new object[] { new { ts = DateTime.UtcNow } });
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true || 
                                               ex.InnerException?.Message.Contains("duplicate") == true)
            {
                TempData["Error"] = "Duplicate detected: This student is already enrolled in this course for this term.";
            }
            catch (DbUpdateException ex)
            {
                TempData["Error"] = $"Error updating enrollment: {ex.InnerException?.Message ?? ex.Message}";
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error saving changes: {ex.Message}");
            }
        }
        else
        {
            // Log validation errors for debugging
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine($"Validation Error: {error.ErrorMessage}");
            }
            TempData["Error"] = "Please fix the validation errors below.";
        }

        ViewData["StudentId"] = new SelectList(_context.Students.OrderBy(s => s.Name), "Id", "Name", existing.StudentId);
        ViewData["CourseId"]  = new SelectList(_context.Courses.OrderBy(c => c.Title), "Id", "Title", existing.CourseId);
        return View(existing);
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
        var enrollment = await _context.Enrollments
            .Include(e => e.Student)
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == id);
            
        if (enrollment == null) return NotFound();

        try
        {
            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();
            
            // Recalculate student GPA after deletion
            await RecalculateGpaAsync(enrollment.StudentId);
            
            TempData["Success"] = $"Enrollment deleted successfully!";
            await _hub.Clients.All.SendCoreAsync("metricsUpdated", new object[] { new { ts = DateTime.UtcNow } });
        }
        catch (DbUpdateException ex)
        {
            TempData["Error"] = $"Cannot delete enrollment. Error: {ex.InnerException?.Message ?? ex.Message}";
        }
        
        return RedirectToAction(nameof(Index));
    }

    // -------- Helper Term Logic --------
    private static void NormalizeIncomingTerm(Enrollment e)
    {
        e.Term = (e.Term ?? "").Trim();
    }

    private static bool IsCurrentTerm(string term)
        => term.Equals("Fall 2025", StringComparison.OrdinalIgnoreCase);

    private static bool IsPastTerm(string term)
        => !string.IsNullOrWhiteSpace(term) 
           && term.EndsWith("2025") 
           && !IsCurrentTerm(term) 
           && (term.Contains("Spring", StringComparison.OrdinalIgnoreCase) 
               || term.Contains("Summer", StringComparison.OrdinalIgnoreCase));

    // GPA recalculation helper: average of numeric grades on non-dropped enrollments
    private async Task RecalculateGpaAsync(int studentId)
    {
        var student = await _context.Students
            .Include(s => s.Enrollments)
                .ThenInclude(e => e.Course)
            .FirstOrDefaultAsync(s => s.Id == studentId);
        if (student == null) return;

        var graded = student.Enrollments
            .Where(e => e.Numeric_Grade.HasValue && e.Status != EnrollmentStatus.Dropped && e.Course != null)
            .ToList();

        // Calculate GPA
        student.Gpa = graded.Any() ? graded.Average(e => e.Numeric_Grade!.Value) : 0m;
        
        _context.Update(student);
        await _context.SaveChangesAsync();
    }
}
