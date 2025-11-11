using System.Collections.Generic;

namespace EduvisionMvc.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int TotalStudents { get; set; }
    public int TotalInstructors { get; set; }
    public int TotalCourses { get; set; }
    public int ActiveEnrollments { get; set; }
    public decimal AverageGpa { get; set; }
    public int PendingApprovals { get; set; }
    public int MaterialsCount { get; set; }
    public int DiscussionsCount { get; set; }
    public int AssignmentsCount { get; set; }
    public List<RoleCount> RoleDistribution { get; set; } = new();
    public List<CourseCapacitySummary> CapacityAlerts { get; set; } = new();
    public List<RecentNotificationSummary> RecentNotifications { get; set; } = new();
    
    // Scatter chart data: Course Capacity vs Current Enrollment
    public List<ScatterDataPoint> CourseCapacityData { get; set; } = new();
}

public class RoleCount { public string Role { get; set; } = ""; public int Count { get; set; } }

public class CourseCapacitySummary
{
    public int CourseId { get; set; }
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public int Capacity { get; set; }
    public int Current { get; set; }
}

public class RecentNotificationSummary
{
    public string UserEmail { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class ScatterDataPoint
{
    public string CourseCode { get; set; } = "";
    public string DepartmentCode { get; set; } = "";
    public int X { get; set; } // Capacity
    public int Y { get; set; } // Current Enrollment
    public double UtilizationRate { get; set; } // Y/X percentage
}
