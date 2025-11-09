using System.Collections.Generic;
using EduvisionMvc.Models;

namespace EduvisionMvc.ViewModels;

public class InstructorDashboardViewModel
{
    // Instructor Info
    public int InstructorId { get; set; }
    public string Name { get; set; } = "";
    public string Department { get; set; } = "";
    public string Email { get; set; } = "";
    public int TotalCourses { get; set; }
    public int ActiveStudents { get; set; }

    // Charts Data
    public List<string> CourseLabels { get; set; } = new();
    public List<int> EnrollmentCounts { get; set; } = new();
    public List<string> GradeLabels { get; set; } = new();
    public List<int> GradeDistribution { get; set; } = new();
    
    // Polar Area Chart: Grade comparison across courses
    public List<PolarChartData> CourseGradeComparison { get; set; } = new();

    // Course Lists
    public List<CourseStatistics> CurrentCourses { get; set; } = new();
    public List<CourseHistory> PastCourses { get; set; } = new();
    public List<StudentPerformance> TopStudents { get; set; } = new();

    // Notifications and Materials
    public List<Notification> RecentNotifications { get; set; } = new();
    public List<CourseMaterial> RecentMaterials { get; set; } = new();
}

public class CourseStatistics
{
    public int CourseId { get; set; }
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public string Term { get; set; } = "";
    public int EnrollmentCount { get; set; }
    public decimal AverageGrade { get; set; }
    public int Credits { get; set; }
    public string Schedule { get; set; } = "";
    public int MaterialsCount { get; set; }
}

public class CourseHistory
{
    public int CourseId { get; set; }
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public string Term { get; set; } = "";
    public int TotalStudents { get; set; }
    public decimal AverageGrade { get; set; }
    public decimal PassRate { get; set; }
}

public class StudentPerformance
{
    public int StudentId { get; set; }
    public string Name { get; set; } = "";
    public string Major { get; set; } = "";
    public decimal GradeAverage { get; set; }
    public int CoursesCompleted { get; set; }
    public List<string> CompletedCourses { get; set; } = new();
}

