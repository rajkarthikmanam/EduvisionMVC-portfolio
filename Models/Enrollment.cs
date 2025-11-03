namespace EduvisionMvc.Models;

public class Enrollment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public string Term { get; set; } = "";
    public decimal Numeric_Grade { get; set; }

    public Student? Student { get; set; }
    public Course? Course { get; set; }
}
