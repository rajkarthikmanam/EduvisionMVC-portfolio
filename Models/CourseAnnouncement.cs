namespace EduvisionMvc.Models;

public class CourseAnnouncement
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course? Course { get; set; }
    public string AuthorId { get; set; } = "";
    public ApplicationUser? Author { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
