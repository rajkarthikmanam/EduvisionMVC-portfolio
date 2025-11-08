namespace EduvisionMvc.Models;

public class Department
{
    public int Id { get; set; }
    public string Code { get; set; } = "";  // e.g., CS
    public string Name { get; set; } = "";  // e.g., Computer Science
    public List<Course> Courses { get; set; } = new List<Course>();
}
