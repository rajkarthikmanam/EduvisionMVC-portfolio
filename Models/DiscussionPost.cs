namespace EduvisionMvc.Models;

public class DiscussionPost
{
    public int Id { get; set; }
    public int DiscussionId { get; set; }
    public Discussion? Discussion { get; set; }
    public string AuthorId { get; set; } = "";
    public ApplicationUser? Author { get; set; }
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
