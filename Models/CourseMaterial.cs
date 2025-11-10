namespace EduvisionMvc.Models;

public class CourseMaterial
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Url { get; set; }
    public string? FilePath { get; set; }
    public MaterialType Type { get; set; } = MaterialType.Document;
    public long FileSize { get; set; } = 0;
    
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
    public bool IsPublished { get; set; } = true;
    
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    
    public string UploadedById { get; set; } = "";
    public ApplicationUser UploadedBy { get; set; } = null!;
}

public enum MaterialType
{
    Document,
    Video,
    Link,
    Presentation,
    Code,
    Other
}