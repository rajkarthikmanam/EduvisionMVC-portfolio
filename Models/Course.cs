namespace EduvisionMvc.Models;

using System.ComponentModel.DataAnnotations;

public class Course
{
    public int Id { get; set; }
    
    // Course details
    [Required, StringLength(20)]
    public string Code { get; set; } = "";
    [Required, StringLength(120)]
    public string Title { get; set; } = "";
    [StringLength(1000)]
    public string Description { get; set; } = "";
    [Range(0, 10)]
    public int Credits { get; set; }
    [Range(1, 500)]
    public int Capacity { get; set; }
    public bool RequiresApproval { get; set; }
    [StringLength(50)]
    public string? Level { get; set; } // Introductory, Intermediate, Advanced
    [StringLength(50)]
    public string? DeliveryMode { get; set; } // Online, In-Person, Hybrid
    [StringLength(200)]
    public string? Prerequisites { get; set; }
    
    // Department relationship
    [Required]
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }

    // Schedule details
    [StringLength(100)]
    public string? Schedule { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    [StringLength(200)]
    public string? Location { get; set; }

    // Navigation properties
    public List<Enrollment> Enrollments { get; set; } = new();
    public List<CourseMaterial> Materials { get; set; } = new();
    public List<CourseInstructor> CourseInstructors { get; set; } = new();
    public List<Assignment> Assignments { get; set; } = new();
    public List<Discussion> Discussions { get; set; } = new();
    public List<CourseAnnouncement> Announcements { get; set; } = new();

    // Computed properties
    public int CurrentEnrollments => Enrollments.Count(e => 
        e.Status == EnrollmentStatus.Approved || e.Status == EnrollmentStatus.Completed);

    public bool IsFull => CurrentEnrollments >= Capacity;
    
    public decimal? AverageGrade => Enrollments
        .Where(e => e.NumericGrade.HasValue)
        .Select(e => e.NumericGrade!.Value)
        .DefaultIfEmpty()
        .Average();

    // Backwards-compatible alias used by some views
    public string Dept => Department?.Name ?? string.Empty;
}
