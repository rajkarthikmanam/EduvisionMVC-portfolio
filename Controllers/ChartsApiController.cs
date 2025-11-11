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
                    .Where(e => e.Numeric_Grade != null)  // Only completed courses with grades
                    .AsEnumerable()
                    .GroupBy(e => e.Course?.Code ?? "Unknown")
                    .Select(g => new
                    {
                        code = g.Key,
                        avg = Math.Round(g.Average(x => (double)x.Numeric_Grade!.Value), 2)
                    })
                    .OrderBy(x => x.code)
                    .ToList()
            );

            return Ok(q);
        }
        // ✅ GET: /api/chartsapi/courseCapacity (capacity vs current active enrollments)
        [HttpGet("courseCapacity")]
        public async Task<IActionResult> GetCourseCapacity()
        {
            var data = await _db.Courses
                .Include(c => c.Enrollments)
                .Where(c => c.Capacity > 0)
                .Select(c => new {
                    code = c.Code,
                    capacity = c.Capacity,
                    current = c.Enrollments.Count(e => e.Status == Models.EnrollmentStatus.Approved && !e.Numeric_Grade.HasValue),
                    utilization = c.Capacity == 0 ? 0 : Math.Round(100.0 * c.Enrollments.Count(e => e.Status == Models.EnrollmentStatus.Approved && !e.Numeric_Grade.HasValue) / c.Capacity, 1)
                })
                .OrderByDescending(x => x.utilization)
                .ToListAsync();
            return Ok(data);
        }

        // ✅ GET: /api/chartsapi/gradeDistribution (A-F counts)
        [HttpGet("gradeDistribution")]
        public async Task<IActionResult> GetGradeDistribution()
        {
            var grades = await _db.Enrollments
                .Where(e => e.Numeric_Grade.HasValue)
                .Select(e => e.Numeric_Grade!.Value)
                .ToListAsync();

            int a = grades.Count(g => g >= 3.7m);
            int b = grades.Count(g => g >= 2.7m && g < 3.7m);
            int c = grades.Count(g => g >= 1.7m && g < 2.7m);
            int d = grades.Count(g => g >= 0.7m && g < 1.7m);
            int f = grades.Count(g => g < 0.7m);

            return Ok(new { labels = new[]{"A","B","C","D","F"}, data = new[]{a,b,c,d,f} });
        }

        // ✅ GET: /api/chartsapi/roleDistribution (counts per Identity role)
        [HttpGet("roleDistribution")]
        public async Task<IActionResult> GetRoleDistribution([FromServices] Microsoft.AspNetCore.Identity.UserManager<Models.ApplicationUser> userManager, [FromServices] Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole> roleManager)
        {
            var roles = roleManager.Roles.Select(r => r.Name).ToList();
            var result = new List<object>();
            foreach (var role in roles)
            {
                var users = await userManager.GetUsersInRoleAsync(role!);
                result.Add(new { role, count = users.Count });
            }
            return Ok(result);
        }

        // ✅ GET: /api/chartsapi/enrollmentHeatmap (per department active enrollments)
        [HttpGet("enrollmentHeatmap")]
        public async Task<IActionResult> GetEnrollmentHeatmap()
        {
            var data = await _db.Departments
                .Select(d => new {
                    department = d.Name,
                    active = d.Courses
                        .SelectMany(c => c.Enrollments)
                        .Count(e => e.Status == Models.EnrollmentStatus.Approved && !e.Numeric_Grade.HasValue),
                    completed = d.Courses
                        .SelectMany(c => c.Enrollments)
                        .Count(e => e.Status == Models.EnrollmentStatus.Completed && e.Numeric_Grade.HasValue)
                })
                .OrderByDescending(x => x.active + x.completed)
                .ToListAsync();
            return Ok(data);
        }
    }
}
