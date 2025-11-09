namespace EduvisionMvc.Models;

public class EnrollmentHistory
{
    public int Id { get; set; }
    public int EnrollmentId { get; set; }
    public Enrollment Enrollment { get; set; } = null!;
    public string Action { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string UpdatedById { get; set; } = "";
    public ApplicationUser UpdatedBy { get; set; } = null!;
    public decimal? OldGrade { get; set; }
    public decimal? NewGrade { get; set; }
}