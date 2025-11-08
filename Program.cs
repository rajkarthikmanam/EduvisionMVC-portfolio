using EduvisionMvc.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
var builder = WebApplication.CreateBuilder(args);

// --- Database context ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- MVC setup ---
builder.Services.AddControllersWithViews();

// --- HttpClient setup ---
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("openlib", c =>
{
    c.BaseAddress = new Uri("https://openlibrary.org/");
    c.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

// --- Apply migrations & ensure DB exists ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// --- Middleware ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// --- Chart API endpoint ---
app.MapGet("/api/charts/gradesByCourse", async (AppDbContext db) =>
{
    var q = await db.Enrollments
        .Include(e => e.Course)
        .GroupBy(e => e.Course!.Code)
        .Select(g => new
        {
            code = g.Key,
            avg = Math.Round(g.Average(x => Convert.ToDouble(x.Numeric_Grade)), 2)
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
