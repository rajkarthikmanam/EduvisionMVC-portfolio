using System.ComponentModel.DataAnnotations.Schema;

namespace EduvisionMvc.Models;

public class Instructor
{
    public int Id { get; set; }

    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";

    // NEW: FK to Department (matches the ERD)
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
