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
    public string Term { get; set; } = "";
    public EnrollmentStatus Status { get; set; }
    public decimal? Numeric_Grade { get; set; }
    public string? LetterGrade { get; set; }
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
    public bool IsActive => Status == EnrollmentStatus.Approved && !Numeric_Grade.HasValue;
    public bool IsCompleted => Status == EnrollmentStatus.Completed && Numeric_Grade.HasValue;

    // Helper method to calculate letter grade
    public void UpdateLetterGrade()
    {
        if (!Numeric_Grade.HasValue)
        {
            LetterGrade = null;
            return;
        }

        LetterGrade = Numeric_Grade.Value switch
        {
            >= 3.7m => "A",
            >= 3.3m => "A-",
            >= 3.0m => "B+",
            >= 2.7m => "B",
            >= 2.3m => "B-",
            >= 2.0m => "C+",
            >= 1.7m => "C",
            >= 1.3m => "C-",
            >= 1.0m => "D+",
            >= 0.7m => "D",
            _ => "F"
        };
    }
}
