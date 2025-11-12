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
        private readonly ILogger<AdminDashboardApiController> _logger;

        public AdminDashboardApiController(AppDbContext db, ILogger<AdminDashboardApiController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // Simple test endpoint
        [HttpGet("test")]
        public async Task<IActionResult> Test()
        {
            try
            {
                var count = await _db.Enrollments.CountAsync();
                var terms = await _db.Enrollments.Select(e => e.Term).Distinct().ToListAsync();
                return Ok(new { 
                    message = "API working", 
                    enrollmentCount = count, 
                    distinctTerms = terms,
                    timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test endpoint failed");
                return StatusCode(500, new { error = ex.Message, type = ex.GetType().Name, stack = ex.StackTrace });
            }
        }

        // GET: /api/dashboard/admin/trend
        // Returns enrollment counts grouped by Term (sorted chronologically when possible)
        [HttpGet("trend")]
        public async Task<IActionResult> GetEnrollmentTrend()
        {
            try
            {
                // Load all terms into memory first
                var allTerms = await _db.Enrollments.Select(e => e.Term).ToListAsync();
                
                // Group in-memory using LINQ
                var grouped = allTerms
                    .GroupBy(t => t)
                    .Select(g => new { term = g.Key, count = g.Count() })
                    .OrderBy(x => x.term)
                    .ToList();

                var labels = grouped.Select(x => x.term).ToArray();
                var data = grouped.Select(x => x.count).ToArray();

                return Ok(new { labels, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetEnrollmentTrend failed");
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace, type = ex.GetType().Name, innerError = ex.InnerException?.Message });
            }
        }

        // GET: /api/dashboard/admin/departments
        // Returns number of courses per department for pie chart
        [HttpGet("departments")]
        public async Task<IActionResult> GetDepartmentDistribution()
        {
            try
            {
                // Load departments and courses separately to avoid MARS issues
                var departments = await _db.Departments.Include(d => d.Courses).ToListAsync();
                
                var data = departments
                    .Select(d => new
                    {
                        dept = d.Code,
                        count = d.Courses.Count
                    })
                    .OrderByDescending(x => x.count)
                    .ToList();

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace, type = ex.GetType().Name });
            }
        }

        // GET: /api/dashboard/admin/capacity
        // Returns bar chart data and alerts for courses with >=80% utilization
        [HttpGet("capacity")]
        public async Task<IActionResult> GetCapacity()
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace, type = ex.GetType().Name });
            }
        }
    }
}
