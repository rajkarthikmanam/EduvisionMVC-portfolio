namespace EduvisionMvc.Models;

public class Assignment
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course? Course { get; set; }
    
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Instructions { get; set; }
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public int MaxPoints { get; set; } = 100;
    public bool IsPublished { get; set; } = true;
    
    public List<AssignmentSubmission> Submissions { get; set; } = new();
}
