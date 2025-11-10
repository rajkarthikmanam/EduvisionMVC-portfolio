using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduvisionMvc.Models;

public class Instructor
{
    public int Id { get; set; }
    [Required, StringLength(50)]
    public string FirstName { get; set; } = "";
    [Required, StringLength(50)]
    public string LastName { get; set; } = "";
    [Required, EmailAddress]
    public string Email { get; set; } = "";
    public string? Title { get; set; } // e.g., Assistant Professor
    public string? OfficeLocation { get; set; }
    [Phone]
    public string? Phone { get; set; }
    public string? OfficeHours { get; set; }
    public DateTime? HireDate { get; set; }
    public string? Bio { get; set; }
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    // Department relationship
    [Required]
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }

    // many-to-many via join
    [NotMapped]
public string DisplayName => $"{FirstName} {LastName}".Trim();

    public List<CourseInstructor> CourseInstructors { get; set; } = new();
}

public class CourseInstructor
{
    public int CourseId { get; set; }
    public int InstructorId { get; set; }
    public Course? Course { get; set; }
    public Instructor? Instructor { get; set; }
}
