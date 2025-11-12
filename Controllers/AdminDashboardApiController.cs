using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduvisionMvc.Data;
using System.Linq;

namespace EduvisionMvc.Controllers
{
    [ApiController]
    [Route("api/dashboard/admin")]
    [AllowAnonymous]
    public class AdminDashboardApiController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AdminDashboardApiController(AppDbContext db)
        {
            _db = db;
        }

        // GET: /api/dashboard/admin/trend
        // Returns enrollment counts grouped by Term (sorted chronologically when possible)
        [HttpGet("trend")]
        public async Task<IActionResult> GetEnrollmentTrend()
        {
            // Group by Term; attempt a basic chronological sort by parsing year then term prefix
            var raw = await _db.Enrollments
                .GroupBy(e => e.Term)
                .Select(g => new { term = g.Key!, count = g.Count() })
                .ToListAsync();

            int TermOrder(string term)
            {
                // Expect formats like "Fall 2025", "Spring 2025"; fallback to alphabetical
                if (string.IsNullOrWhiteSpace(term)) return int.MaxValue;
                var parts = term.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length >= 2 && int.TryParse(parts[^1], out var year))
                {
                    var season = string.Join(' ', parts.Take(parts.Length - 1));
                    var seasonVal = season.ToLower() switch
                    {
                        "winter" => 1,
                        "spring" => 2,
                        "summer" => 3,
                        "fall" => 4,
                        _ => 5
                    };
                    return year * 10 + seasonVal;
                }
                return int.MaxValue - 1;
            }

            var ordered = raw.OrderBy(x => TermOrder(x.term)).ToList();
            var labels = ordered.Select(x => x.term).ToArray();
            var data = ordered.Select(x => x.count).ToArray();

            return Ok(new { labels, data });
        }

        // GET: /api/dashboard/admin/departments
        // Returns number of courses per department for pie chart
        [HttpGet("departments")]
        public async Task<IActionResult> GetDepartmentDistribution()
        {
            var data = await _db.Departments
                .Select(d => new
                {
                    dept = d.Code,
                    count = d.Courses.Count()
                })
                .OrderByDescending(x => x.count)
                .ToListAsync();

            return Ok(data);
        }

        // GET: /api/dashboard/admin/capacity
        // Returns bar chart data and alerts for courses with >=80% utilization
        [HttpGet("capacity")]
        public async Task<IActionResult> GetCapacity()
        {
            var courses = await _db.Courses
                .Include(c => c.Enrollments)
                .ToListAsync();

            var labels = courses.Select(c => c.Code).ToArray();
            var current = courses.Select(c => c.Enrollments.Count(e => e.Term == "Fall 2025" && (e.Status == Models.EnrollmentStatus.Approved || e.Status == Models.EnrollmentStatus.Pending))).ToArray();
            var capacity = courses.Select(c => c.Capacity).ToArray();

            var alerts = courses
                .Where(c => c.Capacity > 0)
                .Select(c => new
                {
                    code = c.Code,
                    title = c.Title,
                    capacity = c.Capacity,
                    current = c.Enrollments.Count(e => e.Term == "Fall 2025" && (e.Status == Models.EnrollmentStatus.Approved || e.Status == Models.EnrollmentStatus.Pending)),
                    util = c.Capacity == 0 ? 0 : (int)Math.Round(100.0 * c.Enrollments.Count(e => e.Term == "Fall 2025" && (e.Status == Models.EnrollmentStatus.Approved || e.Status == Models.EnrollmentStatus.Pending)) / c.Capacity)
                })
                .Where(a => a.util >= 80)
                .OrderByDescending(a => a.util)
                .Take(25)
                .ToList();

            return Ok(new { labels, current, capacity, alerts });
        }
    }
}
