using System.Diagnostics;
using EduvisionMvc.Data;
using EduvisionMvc.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EduvisionMvc.Services;

public class DashboardMetricsService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IHubContext<DashboardHub> _hub;
    private readonly ILogger<DashboardMetricsService> _logger;

    public DashboardMetricsService(IServiceProvider services, IHubContext<DashboardHub> hub, ILogger<DashboardMetricsService> logger)
    {
        _services = services;
        _hub = hub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // small delay to let app warm up
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Compute lightweight metrics for dashboards
                var now = DateTime.UtcNow;
                var lastHour = now.AddHours(-1);

                var students = await db.Students.CountAsync(stoppingToken);
                var courses = await db.Courses.CountAsync(stoppingToken);
                var enrollments = await db.Enrollments.CountAsync(stoppingToken);
                var completed = await db.Enrollments.CountAsync(e => e.Numeric_Grade != null, stoppingToken);
                var activeLastHour = await db.Enrollments.CountAsync(e => e.LastAccessDate != null && e.LastAccessDate >= lastHour, stoppingToken);

                var avgGpa = await db.Students
                    .Where(s => s.Gpa > 0)
                    .Select(s => s.Gpa)
                    .DefaultIfEmpty()
                    .AverageAsync(stoppingToken);

                var deptBreakdown = await db.Courses
                    .Include(c => c.Department)
                    .GroupBy(c => c.Department!.Code)
                    .Select(g => new { dept = g.Key, count = g.Count() })
                    .ToListAsync(stoppingToken);

                var payload = new
                {
                    ts = now,
                    totals = new { students, courses, enrollments, completed, activeLastHour, avgGpa },
                    departments = deptBreakdown
                };

                await _hub.Clients.All.SendAsync("metricsUpdated", payload, cancellationToken: stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "DashboardMetricsService error");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }
}
