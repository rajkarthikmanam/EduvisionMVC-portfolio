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
        options.UseSqlServer(connectionString);
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
    // GLOBAL POLICY: Require authentication by default for ALL endpoints
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    
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
    
    // Apply migrations to create/update database schema
    db.Database.Migrate();
    
    // Seed roles and users if needed
    IdentitySeeder.SeedAsync(scope.ServiceProvider).GetAwaiter().GetResult();
}

// --- Seed roles and admin user (Development only - Production seeds during migration block above) ---
if (app.Environment.IsDevelopment())
{
    IdentitySeeder.SeedAsync(app.Services).GetAwaiter().GetResult();
}

// --- Seed richer LMS domain data ---
// DISABLED: Uncomment only for initial setup, otherwise data gets reset on every restart
// using (var scope2 = app.Services.CreateScope())
// {
//     var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
//     LmsDataSeeder.SeedAsync(db2).GetAwaiter().GetResult();
// }

// --- Middleware ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

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

app.Run();
