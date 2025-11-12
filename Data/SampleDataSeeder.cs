using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EduvisionMvc.Models;

namespace EduvisionMvc.Data;

public static class SampleDataSeeder
{
    public static async Task SeedAsync(AppDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        // Only seed if tables are empty
        if (await db.Departments.AnyAsync()) return;

        // STEP 1: Create 5 Departments
        var depts = new List<Department>
        {
            new() { Code = "CS", Name = "Computer Science", OfficeLocation = "Tech 201", Phone = "555-1001", Email = "cs@edu.local", Description = "CS Dept" },
            new() { Code = "MATH", Name = "Mathematics", OfficeLocation = "Science 305", Phone = "555-1002", Email = "math@edu.local", Description = "Math Dept" },
            new() { Code = "PHYS", Name = "Physics", OfficeLocation = "Science 102", Phone = "555-1003", Email = "physics@edu.local", Description = "Physics Dept" },
            new() { Code = "BUS", Name = "Business", OfficeLocation = "Commerce 401", Phone = "555-1004", Email = "business@edu.local", Description = "Business Dept" },
            new() { Code = "ENG", Name = "English", OfficeLocation = "Arts 220", Phone = "555-1005", Email = "english@edu.local", Description = "English Dept" }
        };
        db.Departments.AddRange(depts);
        await db.SaveChangesAsync();

        // STEP 2: Create 5 Instructors with users
        var instructorRole = "Instructor";
        if (!await roleManager.RoleExistsAsync(instructorRole))
        {
            await roleManager.CreateAsync(new IdentityRole(instructorRole));
        }

        var instructors = new List<Instructor>();
        var instructorEmails = new[] { "inst1@edu.local", "inst2@edu.local", "inst3@edu.local", "inst4@edu.local", "inst5@edu.local" };
        var instructorNames = new[] { ("Sarah", "Johnson"), ("Michael", "Chen"), ("Emily", "Rodriguez"), ("David", "Thompson"), ("Jennifer", "Williams") };

        for (int i = 0; i < 5; i++)
        {
            var email = instructorEmails[i];
            var (firstName, lastName) = instructorNames[i];
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = firstName,
                    LastName = lastName,
                    CreatedAt = DateTime.UtcNow
                };
                await userManager.CreateAsync(user, "Instructor123!");
                await userManager.AddToRoleAsync(user, instructorRole);
            }

            var instructor = new Instructor
            {
                UserId = user.Id,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                DepartmentId = depts[i].Id,
                Title = "Professor",
                OfficeLocation = $"Office {i + 1}",
                Phone = $"555-200{i}",
                OfficeHours = "MW 2-4pm",
                HireDate = DateTime.UtcNow.AddYears(-5),
                Bio = $"Experienced professor in {depts[i].Name}"
            };
            db.Instructors.Add(instructor);
            await db.SaveChangesAsync();

            user.InstructorId = instructor.Id;
            await userManager.UpdateAsync(user);

            instructors.Add(instructor);
        }

        // STEP 3: Create 5 Students with users
        var studentRole = "Student";
        if (!await roleManager.RoleExistsAsync(studentRole))
        {
            await roleManager.CreateAsync(new IdentityRole(studentRole));
        }

        var students = new List<Student>();
        var studentEmails = new[] { "student1@edu.local", "student2@edu.local", "student3@edu.local", "student4@edu.local", "student5@edu.local" };
        var studentNames = new[] { ("Alex", "Martinez"), ("Jessica", "Taylor"), ("Ryan", "Anderson"), ("Sophia", "White"), ("Daniel", "Brown") };

        for (int i = 0; i < 5; i++)
        {
            var email = studentEmails[i];
            var (firstName, lastName) = studentNames[i];
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = firstName,
                    LastName = lastName,
                    CreatedAt = DateTime.UtcNow
                };
                await userManager.CreateAsync(user, "Student123!");
                await userManager.AddToRoleAsync(user, studentRole);
            }

            var student = new Student
            {
                UserId = user.Id,
                Name = $"{firstName} {lastName}",
                Email = email,
                Major = depts[i].Name,
                DepartmentId = depts[i].Id,
                AdvisorInstructorId = instructors[i].Id,
                Phone = $"555-300{i}",
                AcademicLevel = "Junior",
                Gpa = 3.5m + (i * 0.1m),
                TotalCredits = 60 + (i * 5),
                EnrollmentDate = DateTime.UtcNow.AddYears(-2),
                Age = 20 + i
            };
            db.Students.Add(student);
            await db.SaveChangesAsync();

            user.StudentId = student.Id;
            await userManager.UpdateAsync(user);

            students.Add(student);
        }

        // STEP 4: Create 5 Courses
        var courses = new List<Course>();
        var courseCodes = new[] { "CS101", "MATH201", "PHYS301", "BUS250", "ENG150" };
        var courseTitles = new[] { "Intro to Programming", "Calculus II", "Quantum Mechanics", "Marketing Fundamentals", "Creative Writing" };

        for (int i = 0; i < 5; i++)
        {
            var course = new Course
            {
                Code = courseCodes[i],
                Title = courseTitles[i],
                Description = $"Comprehensive course in {courseTitles[i]}",
                Credits = 3 + (i % 2),
                DepartmentId = depts[i].Id,
                Capacity = 25 + (i * 2),
                Level = ((i + 1) * 100).ToString(),
                DeliveryMode = "In-Person",
                Schedule = "MWF 10:00-10:50",
                Location = $"Room {i + 101}",
                StartDate = new DateTime(2025, 8, 25),
                EndDate = new DateTime(2025, 12, 15)
            };
            db.Courses.Add(course);
            await db.SaveChangesAsync();

            // Assign instructor to course
            db.CourseInstructors.Add(new CourseInstructor
            {
                CourseId = course.Id,
                InstructorId = instructors[i].Id
            });

            courses.Add(course);
        }
        await db.SaveChangesAsync();

        // STEP 5: Create 5 Enrollments (mix of approved and pending)
        var enrollments = new List<Enrollment>();
        for (int i = 0; i < 5; i++)
        {
            var enrollment = new Enrollment
            {
                StudentId = students[i].Id,
                CourseId = courses[i].Id,
                Term = "Fall 2025",
                EnrolledDate = DateTime.UtcNow.AddMonths(-2),
                Status = i < 3 ? EnrollmentStatus.Approved : EnrollmentStatus.Pending,
                NumericGrade = i < 2 ? 3.7m + (i * 0.1m) : null,
                ProgressPercentage = i < 2 ? 100 : 50,
                TotalHoursSpent = i < 2 ? 40 : 20
            };
            db.Enrollments.Add(enrollment);
            enrollments.Add(enrollment);
        }
        await db.SaveChangesAsync();

        // Update student total credits
        foreach (var student in students)
        {
            var completedCredits = db.Enrollments
                .Include(e => e.Course)
                .Where(e => e.StudentId == student.Id && e.NumericGrade.HasValue)
                .Sum(e => e.Course!.Credits);
            student.TotalCredits = completedCredits;
        }
        await db.SaveChangesAsync();
    }
}
