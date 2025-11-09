namespace EduvisionMvc.Models;

public class CourseMaterial
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Url { get; set; } = "";
    public DateTime UploadedDate { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public string UploadedById { get; set; } = "";
    public ApplicationUser UploadedBy { get; set; } = null!;
}