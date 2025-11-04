using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduvisionMvc.Data;
using EduvisionMvc.Models;

namespace EduvisionMvc.Controllers
{
    // ⚠️ DEV ONLY — delete this file after seeding.
    [Route("dev/[controller]")]
    public class SeedController : Controller
    {
        private readonly AppDbContext _db;
        public SeedController(AppDbContext db) => _db = db;

        [HttpGet("")]
        public async Task<IActionResult> Run()
        {
            // Load current counts (so we don’t double insert)
            var haveExtra = await _db.Students.AnyAsync(s => s.Id >= 11);
            if (haveExtra)
                return Ok("Seed already applied. Nothing to do.");

            // ---- Departments (6–10) ----
            _db.Departments.AddRange(
                new Department { Id = 6, Name = "Physics" },
                new Department { Id = 7, Name = "Chemistry" },
                new Department { Id = 8, Name = "Psychology" },
                new Department { Id = 9, Name = "Economics" },
                new Department { Id = 10, Name = "Art & Design" }
            );

            // ---- Instructors (11–20) ----
            _db.Instructors.AddRange(
                new Instructor { Id = 11, FirstName = "Richard", LastName = "Feynman", Email = "feynman@univ.edu", DepartmentId = 6 },
                new Instructor { Id = 12, FirstName = "Niels",   LastName = "Bohr",    Email = "bohr@univ.edu",    DepartmentId = 6 },
                new Instructor { Id = 13, FirstName = "Dorothy", LastName = "Hodgkin", Email = "hodgkin@univ.edu", DepartmentId = 7 },
                new Instructor { Id = 14, FirstName = "Linus",   LastName = "Pauling", Email = "pauling@univ.edu", DepartmentId = 7 },
                new Instructor { Id = 15, FirstName = "Daniel",  LastName = "Kahneman",Email = "kahneman@univ.edu",DepartmentId = 8 },
                new Instructor { Id = 16, FirstName = "Amos",    LastName = "Tversky", Email = "tversky@univ.edu", DepartmentId = 8 },
                new Instructor { Id = 17, FirstName = "Paul",    LastName = "Krugman", Email = "krugman@univ.edu", DepartmentId = 9 },
                new Instructor { Id = 18, FirstName = "Esther",  LastName = "Duflo",   Email = "duflo@univ.edu",   DepartmentId = 9 },
                new Instructor { Id = 19, FirstName = "Dieter",  LastName = "Rams",    Email = "rams@univ.edu",    DepartmentId = 10 },
                new Instructor { Id = 20, FirstName = "Paula",   LastName = "Scher",   Email = "scher@univ.edu",   DepartmentId = 10 }
            );

            // ---- Courses (11–20) ----
            _db.Courses.AddRange(
                new Course { Id = 11, Code = "PHY101", Title = "Classical Mechanics",  Credits = 4, DepartmentId = 6 },
                new Course { Id = 12, Code = "PHY220", Title = "Quantum Physics",      Credits = 4, DepartmentId = 6 },
                new Course { Id = 13, Code = "CHM120", Title = "Organic Chemistry I",  Credits = 4, DepartmentId = 7 },
                new Course { Id = 14, Code = "CHM250", Title = "Physical Chemistry",   Credits = 3, DepartmentId = 7 },
                new Course { Id = 15, Code = "PSY101", Title = "Intro to Psychology",  Credits = 3, DepartmentId = 8 },
                new Course { Id = 16, Code = "PSY230", Title = "Cognitive Psychology", Credits = 3, DepartmentId = 8 },
                new Course { Id = 17, Code = "ECO101", Title = "Microeconomics",       Credits = 3, DepartmentId = 9 },
                new Course { Id = 18, Code = "ECO220", Title = "Macroeconomics",       Credits = 3, DepartmentId = 9 },
                new Course { Id = 19, Code = "ART110", Title = "Design Fundamentals",  Credits = 3, DepartmentId = 10 },
                new Course { Id = 20, Code = "ART240", Title = "Information Design",   Credits = 3, DepartmentId = 10 }
            );

            // ---- Students (11–20) ----
            _db.Students.AddRange(
                new Student { Id = 11, Name = "Charlotte", Major = "PHY", Age = 21, Gpa = 3.68m },
                new Student { Id = 12, Name = "Henry",     Major = "CHM", Age = 22, Gpa = 3.44m },
                new Student { Id = 13, Name = "Amelia",    Major = "PSY", Age = 20, Gpa = 3.78m },
                new Student { Id = 14, Name = "Lucas",     Major = "PSY", Age = 21, Gpa = 3.32m },
                new Student { Id = 15, Name = "Mila",      Major = "ECO", Age = 23, Gpa = 3.50m },
                new Student { Id = 16, Name = "Aiden",     Major = "ECO", Age = 22, Gpa = 3.22m },
                new Student { Id = 17, Name = "Harper",    Major = "ART", Age = 20, Gpa = 3.85m },
                new Student { Id = 18, Name = "Evelyn",    Major = "ART", Age = 19, Gpa = 3.60m },
                new Student { Id = 19, Name = "Benjamin",  Major = "PHY", Age = 24, Gpa = 3.28m },
                new Student { Id = 20, Name = "Abigail",   Major = "CHM", Age = 21, Gpa = 3.58m }
            );

            // ---- Enrollments (16–30) ----
            _db.Enrollments.AddRange(
                new Enrollment { Id = 16, StudentId = 11, CourseId = 11, Term = "Fall 2025",   Numeric_Grade = 3.7m },
                new Enrollment { Id = 17, StudentId = 12, CourseId = 13, Term = "Fall 2025",   Numeric_Grade = 3.4m },
                new Enrollment { Id = 18, StudentId = 13, CourseId = 15, Term = "Fall 2025",   Numeric_Grade = 3.9m },
                new Enrollment { Id = 19, StudentId = 14, CourseId = 16, Term = "Fall 2025",   Numeric_Grade = 3.2m },
                new Enrollment { Id = 20, StudentId = 15, CourseId = 17, Term = "Fall 2025",   Numeric_Grade = 3.6m },
                new Enrollment { Id = 21, StudentId = 16, CourseId = 18, Term = "Fall 2025",   Numeric_Grade = 3.1m },
                new Enrollment { Id = 22, StudentId = 17, CourseId = 19, Term = "Fall 2025",   Numeric_Grade = 3.8m },
                new Enrollment { Id = 23, StudentId = 18, CourseId = 20, Term = "Fall 2025",   Numeric_Grade = 3.5m },
                new Enrollment { Id = 24, StudentId = 19, CourseId = 12, Term = "Fall 2025",   Numeric_Grade = 3.3m },
                new Enrollment { Id = 25, StudentId = 20, CourseId = 14, Term = "Fall 2025",   Numeric_Grade = 3.6m },
                new Enrollment { Id = 26, StudentId = 11, CourseId = 12, Term = "Spring 2026", Numeric_Grade = 3.8m },
                new Enrollment { Id = 27, StudentId = 12, CourseId = 14, Term = "Spring 2026", Numeric_Grade = 3.5m },
                new Enrollment { Id = 28, StudentId = 13, CourseId = 16, Term = "Spring 2026", Numeric_Grade = 3.7m },
                new Enrollment { Id = 29, StudentId = 17, CourseId = 20, Term = "Spring 2026", Numeric_Grade = 3.9m },
                new Enrollment { Id = 30, StudentId = 18, CourseId = 19, Term = "Spring 2026", Numeric_Grade = 3.4m }
            );

            await _db.SaveChangesAsync();
            return Ok("Seed inserted ✔  — you can delete Controllers/DevSeedController.cs now.");
        }
    }
}
