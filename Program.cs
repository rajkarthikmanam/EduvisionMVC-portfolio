using EduvisionMvc.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// DbContext (SQLite dev)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// MVC
builder.Services.AddControllersWithViews();

// ✅ HttpClient for API (and a named client)
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("openlib", c =>
{
    c.BaseAddress = new Uri("https://openlibrary.org/");
    c.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

// (Dev) make sure DB exists & migrations apply
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

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

<<<<<<< HEAD
// (Optional) JSON for chart from Enrollments DB data
// Karthik: JSON for Chart.js (avg grade by course)`r`n// Karthik: JSON for Chart.js (avg grade by course)`r`napp.MapGet("/api/charts/gradesByCourse", async (AppDbContext db) =>
{
    var q = db.Enrollments
        .Include(e => e.Course)
        .GroupBy(e => e.Course!.Code)
        .Select(g => new { code = g.Key, avg = Math.Round((double)g.Average(x => x.Numeric_Grade), 2) })
        .OrderBy(x => x.code)
        .ToList();

    return Results.Json(new { labels = q.Select(x => x.code), values = q.Select(x => x.avg) });
});

=======
>>>>>>> 9be398a5927e79c2a3ee5d52169757d70303a88b
app.Run();


