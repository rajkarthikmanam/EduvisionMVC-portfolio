namespace EduvisionMvc.Models;

public class Enrollment
{
    public int Id { get; set; }

    // Core relationships
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public int CourseId { get; set; }
    public Course? Course { get; set; }

    // Enrollment details
    public string Term { get; set; } = string.Empty;
    public EnrollmentStatus Status { get; set; }
    // Only numeric grade retained (0.00 - 4.00 scale)
    public decimal? NumericGrade { get; set; }
    public bool IsRepeatAttempt { get; set; }
    public int AttemptNumber { get; set; } = 1;
    public string? Notes { get; set; }

    // Progress tracking
    public decimal ProgressPercentage { get; set; } = 0;
    public DateTime? LastAccessDate { get; set; }
    public int TotalHoursSpent { get; set; } = 0;

    // Timestamps
    public DateTime EnrolledDate { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public DateTime? DroppedDate { get; set; }
    public DateTime? CompletedDate { get; set; }

    // Navigation properties
    public List<EnrollmentHistory> History { get; set; } = new();

    // Computed properties
    public bool IsActive => Status == EnrollmentStatus.Approved && !NumericGrade.HasValue;
    public bool IsCompleted => Status == EnrollmentStatus.Completed && NumericGrade.HasValue;
}
