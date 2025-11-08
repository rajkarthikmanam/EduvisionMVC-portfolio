using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduvisionMvc.Data;

namespace EduvisionMvc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChartsApiController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ChartsApiController(AppDbContext db)
        {
            _db = db;
        }

        // ✅ GET: /api/chartsapi/gradesbycourse
        [HttpGet("gradesByCourse")]
public async Task<IActionResult> GetGradesByCourse()
{
    var q = await Task.Run(() =>
        _db.Enrollments
            .Include(e => e.Course)
            .AsEnumerable()  // forces client-side evaluation
            .GroupBy(e => e.Course?.Code ?? "Unknown")
            .Select(g => new
            {
                code = g.Key,
                avg = Math.Round(g.Average(x => (double)x.Numeric_Grade), 2)
            })
            .OrderBy(x => x.code)
            .ToList()   // 👈 regular ToList (NOT ToListAsync)
    );

    return Ok(q);
}

        }
    }
