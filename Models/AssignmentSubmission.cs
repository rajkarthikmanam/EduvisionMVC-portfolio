namespace EduvisionMvc.Models;

public class AssignmentSubmission
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public Assignment? Assignment { get; set; }
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public DateTime SubmittedAt { get; set; }
    public decimal? Grade { get; set; }
    public string? Content { get; set; }
}
