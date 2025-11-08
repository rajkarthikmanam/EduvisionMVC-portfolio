using Microsoft.EntityFrameworkCore;
using EduvisionMvc.Models;

namespace EduvisionMvc.Data;

public class AppDbContext(DbContextOptions<AppDbContext> opts) : DbContext(opts)
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<CourseInstructor> CourseInstructors => Set<CourseInstructor>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Precision for GPA (3,2) => e.g., 3.75, 4.00
        b.Entity<Student>()
            .Property(x => x.Gpa)
            .HasPrecision(3, 2);

        // Enrollment relationships
        b.Entity<Enrollment>()
            .HasOne(e => e.Student)
            .WithMany(s => s.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Enrollment>()
            .HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Many-to-many Course <-> Instructor via join entity
        b.Entity<CourseInstructor>()
            .HasKey(ci => new { ci.CourseId, ci.InstructorId });

        b.Entity<CourseInstructor>()
            .HasOne(ci => ci.Course)
            .WithMany(c => c.CourseInstructors)
            .HasForeignKey(ci => ci.CourseId);

        b.Entity<CourseInstructor>()
            .HasOne(ci => ci.Instructor)
            .WithMany(i => i.CourseInstructors)
            .HasForeignKey(ci => ci.InstructorId);

        // Data integrity: Course code must be unique inside a Department
        b.Entity<Course>()
            .HasIndex(c => new { c.Code, c.DepartmentId })
            .IsUnique();

        // ---- Seed data ----
        b.Entity<Department>().HasData(
            new Department { Id = 1, Name = "CS"   },
            new Department { Id = 2, Name = "Math" }
        );

       b.Entity<Instructor>().HasData(
  new Instructor { Id = 1, FirstName = "Dr.", LastName = "Gray", Email = "gray@univ.edu", DepartmentId = 1 },
  new Instructor { Id = 2, FirstName = "Dr.", LastName = "Hall", Email = "hall@univ.edu", DepartmentId = 2 }
);

        b.Entity<Course>().HasData(
            new Course { Id = 1, Code = "CS101",  Title = "Intro CS",   Credits = 3, DepartmentId = 1 },
            new Course { Id = 2, Code = "MTH201", Title = "Calculus I", Credits = 4, DepartmentId = 2 }
        );

        b.Entity<Student>().HasData(
            new Student { Id = 1, Name = "Ava",  Major = "CS",   Age = 20, Gpa = 3.70m },
            new Student { Id = 2, Name = "Liam", Major = "Math", Age = 21, Gpa = 3.50m }
        );

        b.Entity<Enrollment>().HasData(
            new Enrollment { Id = 1, StudentId = 1, CourseId = 1, Term = "Fall 2025", Numeric_Grade = 3.8m },
            new Enrollment { Id = 2, StudentId = 2, CourseId = 2, Term = "Fall 2025", Numeric_Grade = 3.6m }
        );
        b.Entity<CourseInstructor>().HasData(
    new CourseInstructor { CourseId = 1, InstructorId = 1 },
    new CourseInstructor { CourseId = 2, InstructorId = 2 }
);

    }
}
