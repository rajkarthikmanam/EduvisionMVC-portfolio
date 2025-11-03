namespace EduvisionMvc.Models;

public class Course
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public int Credits { get; set; }

    // New: FK to Department
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public string Dept { get; set; } = ""; // keep for backwards compat (optional)
    public List<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    // New: many-to-many
    public List<CourseInstructor> CourseInstructors { get; set; } = new();
}
