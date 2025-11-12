using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduvisionMvc.Data;
using EduvisionMvc.Models;
using System.Text;

namespace EduvisionMvc.Controllers;

public class DiagnosticsController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    public DiagnosticsController(
        AppDbContext context, 
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment env,
        IConfiguration config)
    {
        _context = context;
        _userManager = userManager;
        _env = env;
        _config = config;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Counts()
    {
        try
        {
            var counts = new
            {
                Departments = await _context.Departments.CountAsync(),
                Instructors = await _context.Instructors.CountAsync(),
                Students = await _context.Students.CountAsync(),
                Courses = await _context.Courses.CountAsync(),
                Enrollments = await _context.Enrollments.CountAsync(),
                Users = await _context.Users.CountAsync(),
                Roles = await _context.Roles.CountAsync(),
                Provider = _context.Database.ProviderName,
                CanConnect = _context.Database.CanConnect()
            };
            return Json(new { Status = "Success", Counts = counts, Timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            return Json(new { Status = "Error", Message = ex.Message, Inner = ex.InnerException?.Message });
        }
    }

    [AllowAnonymous]
    public async Task<IActionResult> ForceSeed()
    {
        try
        {
            var roleMgr = HttpContext.RequestServices.GetRequiredService<RoleManager<IdentityRole>>();
            await Data.SampleDataSeeder.SeedAsync(_context, _userManager, roleMgr);
            
            var counts = new
            {
                Departments = await _context.Departments.CountAsync(),
                Instructors = await _context.Instructors.CountAsync(),
                Students = await _context.Students.CountAsync(),
                Courses = await _context.Courses.CountAsync(),
                Enrollments = await _context.Enrollments.CountAsync()
            };
            
            return Json(new { Status = "Success", Message = "Seed completed", Counts = counts });
        }
        catch (Exception ex)
        {
            return Json(new { Status = "Error", Message = ex.Message, Stack = ex.StackTrace, Inner = ex.InnerException?.Message });
        }
    }

    [AllowAnonymous]
    public IActionResult Env()
    {
        var connString = _config.GetConnectionString("DefaultConnection") ?? "NOT SET";
        var dbProvider = _context.Database.ProviderName ?? "UNKNOWN";
        
        // Mask password in connection string for security
        var maskedConn = connString;
        if (connString.Contains("Password=", StringComparison.OrdinalIgnoreCase))
        {
            var parts = connString.Split(';');
            maskedConn = string.Join(";", parts.Select(p => 
                p.Trim().StartsWith("Password=", StringComparison.OrdinalIgnoreCase) 
                    ? "Password=***REDACTED***" 
                    : p));
        }
        
        var canConnect = false;
        var dbError = "";
        try
        {
            canConnect = _context.Database.CanConnect();
        }
        catch (Exception ex)
        {
            dbError = ex.Message;
        }

        return Json(new
        {
            environment = _env.EnvironmentName,
            isDevelopment = _env.IsDevelopment(),
            isProduction = _env.IsProduction(),
            dbProvider = dbProvider,
            connectionString = maskedConn,
            canConnectToDb = canConnect,
            dbError = dbError,
            timestamp = DateTime.UtcNow
        });
    }

    [AllowAnonymous]
    public IActionResult Migrations()
    {
        try
        {
            var pendingMigrations = _context.Database.GetPendingMigrations().ToList();
            var appliedMigrations = _context.Database.GetAppliedMigrations().ToList();
            
            return Json(new
            {
                applied = appliedMigrations,
                pending = pendingMigrations,
                hasPending = pendingMigrations.Any(),
                canConnect = _context.Database.CanConnect()
            });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    [AllowAnonymous]
    public IActionResult ApplyMigrations()
    {
        try
        {
            _context.Database.Migrate();
            return Json(new
            {
                success = true,
                message = "Migrations applied successfully",
                applied = _context.Database.GetAppliedMigrations().ToList()
            });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                success = false,
                error = ex.Message,
                innerError = ex.InnerException?.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CheckStudentLinks()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== IDENTITY USERS WITH STUDENT ROLE ===\n");

        var studentUsers = await _userManager.GetUsersInRoleAsync("Student");
        
        foreach (var user in studentUsers)
        {
            sb.AppendLine($"User ID: {user.Id}");
            sb.AppendLine($"UserName: {user.UserName}");
            sb.AppendLine($"Email: {user.Email}");
            sb.AppendLine($"StudentId FK: {user.StudentId}");
            
            // Check if Student record exists with matching UserId
            var studentByUserId = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (studentByUserId != null)
            {
                sb.AppendLine($"✓ Student found by UserId: ID={studentByUserId.Id}, Name={studentByUserId.Name}, Email={studentByUserId.Email}");
            }
            else
            {
                sb.AppendLine($"✗ NO Student record with UserId={user.Id}");
            }
            
            // Check if StudentId FK points to valid Student
            if (user.StudentId.HasValue)
            {
                var studentByFK = await _context.Students.FindAsync(user.StudentId.Value);
                if (studentByFK != null)
                {
                    sb.AppendLine($"  StudentId FK points to: ID={studentByFK.Id}, Name={studentByFK.Name}, UserId={studentByFK.UserId}");
                    if (studentByFK.UserId != user.Id)
                    {
                        sb.AppendLine($"  ⚠ WARNING: Student.UserId ({studentByFK.UserId}) != ApplicationUser.Id ({user.Id})");
                    }
                }
                else
                {
                    sb.AppendLine($"  ✗ StudentId FK ({user.StudentId}) points to NON-EXISTENT Student record!");
                }
            }
            else
            {
                sb.AppendLine($"  StudentId FK is NULL");
            }
            
            sb.AppendLine("---\n");
        }

        return Content(sb.ToString(), "text/plain");
    }
}
