namespace EduvisionMvc.Models;

public class Discussion
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course? Course { get; set; }
    public string Title { get; set; } = "";
    public List<DiscussionPost> Posts { get; set; } = new();
}
