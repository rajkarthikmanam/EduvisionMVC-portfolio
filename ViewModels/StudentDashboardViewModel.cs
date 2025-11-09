using EduvisionMvc.Models;

namespace EduvisionMvc.ViewModels;

public class StudentDashboardViewModel
{
    // Student Info
    public string Name { get; set; } = "";
    public string Major { get; set; } = "";
    public decimal Gpa { get; set; }
    public int TotalCredits { get; set; }
    public int CreditsInProgress { get; set; }

    // Charts Data
    public List<string> GradeLabels { get; set; } = new();
    public List<decimal> GradeData { get; set; } = new();
    public List<string> CourseLabels { get; set; } = new();
    public List<decimal> CoursePerformance { get; set; } = new();
    
    // Course Lists
    public List<EnrollmentSummary> TopCourses { get; set; } = new();
    public List<EnrollmentSummary> WeakCourses { get; set; } = new();
    public List<EnrollmentSummary> CurrentCourses { get; set; } = new();

    // Progress Metrics
    public int RequiredCredits { get; set; } = 120;
    public decimal CompletionPercentage => (TotalCredits * 100m) / RequiredCredits;
    public List<Notification> RecentNotifications { get; set; } = new();
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