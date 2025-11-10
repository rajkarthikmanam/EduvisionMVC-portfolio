using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduvisionMvc.Data;

namespace EduvisionMvc.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public DashboardApiController(AppDbContext db) => _db = db;

    [HttpGet("admin/summary")]
    [Authorize(Roles="Admin")]
    public async Task<IActionResult> GetAdminSummary()
    {
        var students = await _db.Students.CountAsync();
        var courses = await _db.Courses.CountAsync();
        var enrollments = await _db.Enrollments.CountAsync();
        var completed = await _db.Enrollments.CountAsync(e => e.Numeric_Grade != null);
        var avgGpa = await _db.Students.Where(s => s.Gpa > 0).Select(s => s.Gpa).DefaultIfEmpty().AverageAsync();

        var dept = await _db.Courses.Include(c => c.Department)
            .Where(c => c.Department != null)
            .GroupBy(c => c.Department!.Code)
            .Select(g => new { dept = g.Key, courses = g.Count() })
            .ToListAsync();

        return Ok(new { students, courses, enrollments, completed, avgGpa, dept });
    }

    [HttpGet("instructor/{id:int}/courses")]
    [Authorize(Roles="Instructor")]
    public async Task<IActionResult> GetInstructorCourses(int id)
    {
        var courses = await _db.CourseInstructors
            .Where(ci => ci.InstructorId == id && ci.Course != null)
            .Select(ci => new {
                Id = ci.Course!.Id,
                Code = ci.Course!.Code,
                Title = ci.Course!.Title,
                enrollments = ci.Course!.Enrollments.Count,
                avgGrade = ci.Course!.AverageGrade
            }).ToListAsync();
        return Ok(courses);
    }

    [HttpGet("student/{id:int}/performance")]
    [Authorize(Roles="Student")]
    public async Task<IActionResult> GetStudentPerformance(int id)
    {
        var enrollments = await _db.Enrollments
            .Include(e => e.Course)
            .Where(e => e.StudentId == id && e.Course != null)
            .OrderByDescending(e => e.EnrolledDate)
            .Select(e => new {
                Code = e.Course!.Code,
                Title = e.Course!.Title,
                e.Term,
                grade = e.Numeric_Grade,
                progress = e.ProgressPercentage
            })
            .Take(25)
            .ToListAsync();
        return Ok(enrollments);
    }
}
