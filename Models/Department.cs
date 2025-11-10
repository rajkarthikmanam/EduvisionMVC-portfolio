namespace EduvisionMvc.Models;

using System.ComponentModel.DataAnnotations;

public class Department
{
    public int Id { get; set; }
    [Required, StringLength(10)]
    public string Code { get; set; } = "";  // e.g., CS
    [Required, StringLength(120)]
    public string Name { get; set; } = "";  // e.g., Computer Science
    [StringLength(800)]
    public string? Description { get; set; }
    [StringLength(100)]
    public string? OfficeLocation { get; set; }
    [Phone]
    public string? Phone { get; set; }
    [EmailAddress]
    public string? Email { get; set; }
    [Url]
    public string? Website { get; set; }

    // Optional department chair (Instructor)
    public int? ChairId { get; set; }
    public Instructor? Chair { get; set; }
    public List<Course> Courses { get; set; } = new List<Course>();
    public List<Student> Students { get; set; } = new();
}
