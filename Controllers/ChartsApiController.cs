using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduvisionMvc.Data;

namespace EduvisionMvc.Controllers;

[ApiController]
[Route("api/charts")]
public class ChartsApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public ChartsApiController(AppDbContext db) => _db = db;

    [HttpGet("gradesByCourse")]
    public async Task<IActionResult> GradesByCourse()
    {
        var data = await _db.Enrollments
            .Include(e => e.Course)
            .GroupBy(e => e.Course!.Title)
            .Select(g => new { Course = g.Key, AvgGrade = Math.Round(g.Average(e => e.Numeric_Grade), 2) })
            .ToListAsync();

        return Ok(data);
    }
}
