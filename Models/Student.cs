namespace EduvisionMvc.Models;

public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Major { get; set; } = "";
    public int Age { get; set; }
    public decimal Gpa { get; set; }
    public List<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
