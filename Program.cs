using EduvisionMvc.Data;
using EduvisionMvc.Models;
using EduvisionMvc.Authorization;
using EduvisionMvc.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
var builder = WebApplication.CreateBuilder(args);

// --- Database context ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("DefaultConnection string is missing. Configure it in appsettings or Azure App Service settings.");
    }

    // Environment-based provider selection:
    // Development → SQLite (local file database)
    // Production → SQL Server (Azure SQL Database)
    if (builder.Environment.IsDevelopment())
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        // Ensure MARS enabled to avoid DataReader concurrency errors
        var pattern = @"MultipleActiveResultSets=\s*False";
        var sqlConn = connectionString.Contains("MultipleActiveResultSets", StringComparison.OrdinalIgnoreCase)
            ? System.Text.RegularExpressions.Regex.Replace(connectionString, pattern, "MultipleActiveResultSets=True", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            : connectionString.TrimEnd(';') + ";MultipleActiveResultSets=True";
        options.UseSqlServer(sqlConn);
    }
});

// --- Identity & MVC setup ---
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddControllersWithViews();
// --- Real-time SignalR ---
builder.Services.AddSignalR();

// --- Register services ---
builder.Services.AddScoped<ILoginRedirectService, LoginRedirectService>();
builder.Services.AddHostedService<DashboardMetricsService>();

// --- Register admin protection authorization handler ---
builder.Services.AddScoped<IAuthorizationHandler, AdminProtectionHandler>();

builder.Services.AddAuthorization(options =>
{
    // NOTE: No global FallbackPolicy - controllers use [Authorize] or [AllowAnonymous] explicitly
    // This prevents redirect loops on Login/Register pages
    
    // Role-based policies
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireInstructor", policy => policy.RequireRole("Instructor"));
    options.AddPolicy("RequireStudent", policy => policy.RequireRole("Student"));
});

// --- HttpClient setup ---
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("openlib", c =>
{
    c.BaseAddress = new Uri("https://openlibrary.org/");
    c.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

// --- Apply migrations & ensure DB exists (Production only) ---
if (app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Apply migrations to create/update database schema
        db.Database.Migrate();

        // Seed roles and users if needed
        IdentitySeeder.SeedAsync(scope.ServiceProvider).GetAwaiter().GetResult();

        // Seed sample data for dashboard visibility (5 rows per table)
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        SampleDataSeeder.SeedAsync(db, userMgr, roleMgr).GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        // Do not crash the process on startup; log and continue so we can inspect logs via Log Stream
        logger.LogError(ex, "Failed during Production startup migration/seeding. Application will start without DB being migrated.");
    }
}

// --- Seed roles and admin user (Development only - Production seeds during migration block above) ---
if (app.Environment.IsDevelopment())
{
    IdentitySeeder.SeedAsync(app.Services).GetAwaiter().GetResult();
}

// --- Seed sample data (5 rows per table) in Development ---
if (app.Environment.IsDevelopment())
{
    using (var scope2 = app.Services.CreateScope())
    {
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var userMgr = scope2.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleMgr = scope2.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        SampleDataSeeder.SeedAsync(db2, userMgr, roleMgr).GetAwaiter().GetResult();
    }
}

// --- Middleware ---
// Always use exception handler, never show developer exception page in production deployment
app.UseExceptionHandler("/Home/Error");
app.UseHsts();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Enable attribute-routed controllers (e.g., /api/... and /dev/... endpoints)
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// --- SignalR hubs ---
app.MapHub<EduvisionMvc.Hubs.DashboardHub>("/hubs/dashboard");

// --- Chart API endpoint ---
app.MapGet("/api/charts/gradesByCourse", async (AppDbContext db) =>
{
    var q = await db.Enrollments
        .Include(e => e.Course)
        .Where(e => e.NumericGrade != null)
        .GroupBy(e => e.Course!.Code)
        .Select(g => new
        {
            code = g.Key,
            avg = Math.Round(g.Average(x => Convert.ToDouble(x.NumericGrade!.Value)), 2)
        })
        .OrderBy(x => x.code)
        .ToListAsync();

    return Results.Json(new
    {
        labels = q.Select(x => x.code),
        values = q.Select(x => x.avg)
    });
});

// --- Minimal Admin Dashboard APIs (bypass MVC to avoid pipeline issues) ---
app.MapGet("/api/dashboard/admin/trend2", async (AppDbContext db) =>
{
    int TermOrder(string? term)
    {
        if (string.IsNullOrWhiteSpace(term)) return int.MaxValue;
        var parts = term.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length >= 2 && int.TryParse(parts[^1], out var year))
        {
            var season = string.Join(' ', parts.Take(parts.Length - 1)).ToLowerInvariant();
            var seasonVal = season switch
            {
                "winter" => 0,
                "spring" => 1,
                "summer" => 2,
                "fall" => 3,
                _ => 9
            };
            return year * 10 + seasonVal;
        }
        return int.MaxValue - 1;
    }

    var terms = await db.Enrollments.Select(e => e.Term).ToListAsync();
    var grouped = terms
        .GroupBy(t => t)
        .Select(g => new { term = g.Key ?? "(unknown)", count = g.Count() })
        .OrderBy(x => TermOrder(x.term))
        .ToList();

    return Results.Json(new
    {
        labels = grouped.Select(x => x.term).ToArray(),
        data = grouped.Select(x => x.count).ToArray()
    });
});

app.MapGet("/api/dashboard/admin/departments2", async (AppDbContext db) =>
{
    var departments = await db.Departments.Include(d => d.Courses).ToListAsync();
    var data = departments
        .Select(d => new { dept = d.Code, count = d.Courses.Count })
        .OrderByDescending(x => x.count)
        .ToList();
    return Results.Json(data);
});

app.MapGet("/api/dashboard/admin/capacity2", async (AppDbContext db) =>
{
    var courses = await db.Courses.Include(c => c.Enrollments).ToListAsync();
    var labels = courses.Select(c => c.Code).ToArray();
    var current = courses
        .Select(c => c.Enrollments.Count(e => e.Term == "Fall 2025" && (e.Status == EduvisionMvc.Models.EnrollmentStatus.Approved || e.Status == EduvisionMvc.Models.EnrollmentStatus.Pending)))
        .ToArray();
    var capacity = courses.Select(c => c.Capacity).ToArray();

    var alerts = courses
        .Where(c => c.Capacity > 0)
        .Select(c => new
        {
            code = c.Code,
            title = c.Title,
            capacity = c.Capacity,
            current = c.Enrollments.Count(e => e.Term == "Fall 2025" && (e.Status == EduvisionMvc.Models.EnrollmentStatus.Approved || e.Status == EduvisionMvc.Models.EnrollmentStatus.Pending)),
            util = c.Capacity == 0 ? 0 : (int)Math.Round(100.0 * c.Enrollments.Count(e => e.Term == "Fall 2025" && (e.Status == EduvisionMvc.Models.EnrollmentStatus.Approved || e.Status == EduvisionMvc.Models.EnrollmentStatus.Pending)) / c.Capacity)
        })
        .Where(a => a.util >= 80)
        .OrderByDescending(a => a.util)
        .Take(25)
        .ToList();

    return Results.Json(new { labels, current, capacity, alerts });
});

// --- Minimal Student Dashboard Diagnostics ---
// Provides a lean snapshot to troubleshoot production errors without invoking full MVC controller logic.
app.MapGet("/api/dashboard/student/diag", async (AppDbContext db, UserManager<ApplicationUser> userManager, IHttpContextAccessor accessor) =>
{
    try
    {
        var user = accessor.HttpContext?.User != null ? await userManager.GetUserAsync(accessor.HttpContext.User) : null;
        var userId = user?.Id ?? "(anon)";
        var roles = user != null ? await userManager.GetRolesAsync(user) : new List<string>();

        // Robust lookup and auto-heal: prefer StudentId, then UserId; create minimal profile if none and user is in Student role
        Student? student = null;
        if (user != null)
        {
            if (user.StudentId.HasValue)
            {
                student = await db.Students.Include(s => s.Enrollments).ThenInclude(e => e.Course)
                    .FirstOrDefaultAsync(s => s.Id == user.StudentId.Value);
            }
            if (student == null)
            {
                student = await db.Students.Include(s => s.Enrollments).ThenInclude(e => e.Course)
                    .FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (student != null && (!user.StudentId.HasValue || user.StudentId.Value != student.Id))
                {
                    user.StudentId = student.Id;
                    await userManager.UpdateAsync(user);
                }
            }
            if (student == null && roles.Contains("Student"))
            {
                // Create a barebones student profile to unblock dashboard
                var dept = await db.Departments.OrderBy(d => d.Id).FirstOrDefaultAsync();
                if (dept == null)
                {
                    dept = new Department { Name = "General", Code = "GEN" };
                    db.Departments.Add(dept);
                    await db.SaveChangesAsync();
                }
                student = new Student
                {
                    UserId = user.Id,
                    Name = ($"{user.FirstName} {user.LastName}").Trim(),
                    Email = user.Email ?? user.UserName ?? string.Empty,
                    Major = dept.Name,
                    DepartmentId = dept.Id,
                    EnrollmentDate = DateTime.UtcNow.Date,
                    Gpa = 0m,
                    TotalCredits = 120
                };
                db.Students.Add(student);
                await db.SaveChangesAsync();
                user.StudentId = student.Id;
                await userManager.UpdateAsync(user);
            }
        }

        var enrollments = student?.Enrollments ?? new List<Enrollment>();
        var withNullCourse = enrollments.Count(e => e.Course == null);
        var distinctTerms = enrollments.Select(e => e.Term).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().OrderBy(t => t).ToList();
        var completed = enrollments.Count(e => e.NumericGrade.HasValue);
        var active = enrollments.Count(e => !e.NumericGrade.HasValue && e.Status == EnrollmentStatus.Approved);
        var pending = enrollments.Count(e => !e.NumericGrade.HasValue && e.Status == EnrollmentStatus.Pending);
        var dropped = enrollments.Count(e => e.Status == EnrollmentStatus.Dropped);

        // Only count credits from passing grades (>= 1.0 / D or better); failed courses (< 1.0) don't count toward completion
        var creditsCompleted = enrollments.Where(e => e.NumericGrade.HasValue && e.NumericGrade >= 1.0m && e.Course != null).Sum(e => e.Course!.Credits);
        var creditsInProgress = enrollments.Where(e => !e.NumericGrade.HasValue && e.Course != null && (e.Status == EnrollmentStatus.Approved || e.Status == EnrollmentStatus.Pending)).Sum(e => e.Course!.Credits);

        return Results.Json(new
        {
            userId,
            isStudentRole = roles.Contains("Student"),
            hasStudent = student != null,
            enrollmentsTotal = enrollments.Count,
            withNullCourse,
            distinctTerms,
            completed,
            active,
            pending,
            dropped,
            creditsCompleted,
            creditsInProgress,
            requiredCredits = student?.TotalCredits,
            sample = enrollments.Take(5).Select(e => new
            {
                e.Id,
                e.Term,
                e.Status,
                e.NumericGrade,
                courseCode = e.Course?.Code,
                courseCredits = e.Course?.Credits
            }).ToList()
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = ex.Message, stack = ex.StackTrace?.Split('\n').Take(5) });
    }
});

// --- Student Dashboard Live Refresh API (called by client-side JavaScript) ---
app.MapGet("/api/dashboard/student", async (AppDbContext db, UserManager<ApplicationUser> userManager, IHttpContextAccessor accessor) =>
{
    try
    {
        var user = accessor.HttpContext?.User != null ? await userManager.GetUserAsync(accessor.HttpContext.User) : null;
        if (user == null) return Results.Json(new { error = "Not authenticated" });

        Student? student = null;
        if (user.StudentId.HasValue)
        {
            student = await db.Students.Include(s => s.Enrollments).ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(s => s.Id == user.StudentId.Value);
        }
        if (student == null)
        {
            student = await db.Students.Include(s => s.Enrollments).ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);
        }
        if (student == null) return Results.Json(new { error = "No student profile" });

        var enrollments = student.Enrollments ?? new List<Enrollment>();
        
        // Only count credits from PASSING grades (>= 1.0 / D or better); failed courses don't count
        var totalCredits = enrollments
            .Where(e => e.NumericGrade.HasValue && e.NumericGrade >= 1.0m && e.Course != null)
            .Sum(e => e.Course!.Credits);
        
        var creditsInProgress = enrollments
            .Where(e => !e.NumericGrade.HasValue && e.Course != null && e.Status == EnrollmentStatus.Approved)
            .Sum(e => e.Course!.Credits);
        
        var currentCoursesCount = enrollments.Count(e => !e.NumericGrade.HasValue && e.Status == EnrollmentStatus.Approved);
        var completedCourses = enrollments.Count(e => e.NumericGrade.HasValue);

        return Results.Json(new
        {
            totalCredits,
            creditsInProgress,
            requiredCredits = student.TotalCredits,
            currentCoursesCount,
            completedCourses
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = ex.Message });
    }
});

app.Run();
