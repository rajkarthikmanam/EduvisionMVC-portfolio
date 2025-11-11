namespace EduvisionMvc.Models;

using System.ComponentModel.DataAnnotations;

public class Student
{
    public int Id { get; set; }
    
    // Identity and personal info
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    [Required, StringLength(120)]
    public string Name { get; set; } = "";
    [Required, EmailAddress]
    public string Email { get; set; } = "";
    [Required, StringLength(50)]
    public string Major { get; set; } = "";
    [Phone]
    public string? Phone { get; set; }
    [StringLength(40)]
    public string? AcademicLevel { get; set; } // Freshman, Sophomore, etc.
    public int? AdvisorInstructorId { get; set; }
    public Instructor? AdvisorInstructor { get; set; }
    
    // Academic home department (optional for backward compatibility)
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public DateTime EnrollmentDate { get; set; }

    // Academic metrics
    public decimal Gpa { get; set; }
    
    // Total credits required for degree completion (e.g., 120)
    [Range(6, int.MaxValue, ErrorMessage = "Required credits must be at least 6.")]
    public int TotalCredits { get; set; } = 120;
    
    public int Age { get; set; }

    // Navigation properties
    public List<Enrollment> Enrollments { get; set; } = new();
    public List<AssignmentSubmission> Submissions { get; set; } = new();

    // Computed properties
    public int CreditsInProgress => Enrollments
        .Where(e => e.Status == EnrollmentStatus.Approved && !e.Numeric_Grade.HasValue)
        .Sum(e => e.Course?.Credits ?? 0);

    public int CompletedCredits => Enrollments
        .Where(e => e.Status == EnrollmentStatus.Completed && e.Numeric_Grade.HasValue)
        .Sum(e => e.Course?.Credits ?? 0);
}
