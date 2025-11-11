using System.Collections.Generic;
using EduvisionMvc.Models;

namespace EduvisionMvc.ViewModels;

public class StudentDashboardViewModel
{
    // Student Info
    public int StudentId { get; set; }
    public string Name { get; set; } = "";
    public string Major { get; set; } = "";
    public decimal Gpa { get; set; }
    public string? AdvisorName { get; set; }
    public string? Phone { get; set; }
    public string? AcademicLevel { get; set; }
    public int TotalCredits { get; set; } // Completed credits (from enrollments)
    public int CreditsInProgress { get; set; }
    public int CompletedCoursesCount { get; set; } // Count of completed courses

    // Charts Data
    public List<string> GradeLabels { get; set; } = new();
    public List<decimal> GradeData { get; set; } = new();
    public List<string> CourseLabels { get; set; } = new();
    public List<decimal> CoursePerformance { get; set; } = new();
    
    // Radar Chart - Skills/Competencies across subjects
    public RadarChartData? SkillRadarData { get; set; }
    
    // Course Lists
    public List<EnrollmentSummary> TopCourses { get; set; } = new();
    public List<EnrollmentSummary> WeakCourses { get; set; } = new();
    public List<EnrollmentSummary> CompletedCourses { get; set; } = new(); // All completed courses
    public List<EnrollmentSummary> CurrentCourses { get; set; } = new();
    public List<EnrollmentSummary> PendingCourses { get; set; } = new();

    // Progress Metrics
    public int RequiredCredits { get; set; } = 120; // Credits required for graduation (from Student.TotalCredits)
    public decimal CompletionPercentage => (RequiredCredits == 0) ? 0 : (TotalCredits * 100m) / RequiredCredits;
    public List<Notification> RecentNotifications { get; set; } = new();
    
    // Raw enrollments for progress lookup
    public List<Enrollment> AllEnrollments { get; set; } = new();
}

public class EnrollmentSummary
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = "";
    public string CourseTitle { get; set; } = "";
    public int Credits { get; set; }
    public string Term { get; set; } = "";
    public decimal? Grade { get; set; }
    public string InstructorName { get; set; } = "";
    public string Department { get; set; } = "";
}