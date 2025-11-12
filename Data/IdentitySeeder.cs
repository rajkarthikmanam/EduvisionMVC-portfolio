using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using EduvisionMvc.Models;

namespace EduvisionMvc.Data;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        await SeedInternalAsync(services);
    }

    private static async Task SeedInternalAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
    var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var roles = new[] { "Admin", "Instructor", "Student" };
        foreach (var r in roles)
        {
            if (!await roleMgr.RoleExistsAsync(r))
            {
                await roleMgr.CreateAsync(new IdentityRole(r));
            }
        }

        // Admin user
        var adminEmail = "admin@local.test";
        var admin = await userMgr.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "Site",
                LastName = "Admin",
                CreatedAt = DateTime.UtcNow
            };
            var pw = Environment.GetEnvironmentVariable("EDUVISION_ADMIN_PW") ?? "Admin123!";
            var cr = await userMgr.CreateAsync(admin, pw);
            if (!cr.Succeeded)
            {
                // swallow - user can fix password complexity in env if needed
            }
        }

        if (!await userMgr.IsInRoleAsync(admin, "Admin"))
        {
            await userMgr.AddToRoleAsync(admin, "Admin");
        }

        // Create ONLY the core demo instructor account if it's missing
        var demoInstructorEmail = "instructor@local.test";
        var inst = await userMgr.FindByEmailAsync(demoInstructorEmail);
        if (inst == null)
        {
            inst = new ApplicationUser
            {
                UserName = demoInstructorEmail,
                Email = demoInstructorEmail,
                EmailConfirmed = true,
                FirstName = "Demo",
                LastName = "Instructor",
                CreatedAt = DateTime.UtcNow
            };
            await userMgr.CreateAsync(inst, "Instructor123!");
        }
        if (!await userMgr.IsInRoleAsync(inst, "Instructor"))
        {
            await userMgr.AddToRoleAsync(inst, "Instructor");
        }

        // Create ONLY the core demo student account if it's missing
        var studentEmail = "student@local.test";
        var stu = await userMgr.FindByEmailAsync(studentEmail);
        if (stu == null)
        {
            stu = new ApplicationUser
            {
                UserName = studentEmail,
                Email = studentEmail,
                EmailConfirmed = true,
                FirstName = "Demo",
                LastName = "Student",
                CreatedAt = DateTime.UtcNow
            };
            await userMgr.CreateAsync(stu, "Student123!");
        }
        if (!await userMgr.IsInRoleAsync(stu, "Student"))
        {
            await userMgr.AddToRoleAsync(stu, "Student");
        }

        // --- Domain seed to link identities to LMS entities ---
        // ONLY create if database is completely empty (first-time setup)
        // Skip if there's already data to preserve user changes
        // DISABLED: Allow SampleDataSeeder to populate instead
        // if (db.Departments.Any() || db.Students.Any() || db.Courses.Any())
        // {
        //     return;
        // }

        // First-time setup only: Ensure a default Department
        var dept = db.Departments.FirstOrDefault(d => d.Name == "Computer Science");
        if (dept == null)
        {
            dept = new Department { Name = "Computer Science" };
            db.Departments.Add(dept);
            await db.SaveChangesAsync();
        }

        // Ensure Instructor entity linked to instructor user
        var instructorEntity = db.Instructors.FirstOrDefault(i => i.UserId == inst.Id);
        if (instructorEntity == null)
        {
            instructorEntity = new Instructor
            {
                FirstName = inst.FirstName ?? "Demo",
                LastName = inst.LastName ?? "Instructor",
                Email = inst.Email ?? inst.UserName ?? "instructor@local.test",
                DepartmentId = dept.Id,
                UserId = inst.Id
            };
            db.Instructors.Add(instructorEntity);
            await db.SaveChangesAsync();
        }

        // Link ApplicationUser to Instructor via FK on ApplicationUser.InstructorId
        if (inst.InstructorId != instructorEntity.Id)
        {
            inst.InstructorId = instructorEntity.Id;
            await userMgr.UpdateAsync(inst);
        }

        // Ensure Student entity linked to student user
        var studentEntity = db.Students.FirstOrDefault(s => s.UserId == stu.Id);
        if (studentEntity == null)
        {
            studentEntity = new Student
            {
                Name = (stu.FirstName + " " + stu.LastName).Trim(),
                Email = stu.Email ?? stu.UserName ?? "student@local.test",
                Major = "CS",
                EnrollmentDate = DateTime.UtcNow.Date,
                Gpa = 3.5m,
                TotalCredits = 30,
                Age = 21,
                UserId = stu.Id
            };
            db.Students.Add(studentEntity);
            await db.SaveChangesAsync();
        }

        // Link ApplicationUser to Student via FK on ApplicationUser.StudentId
        if (stu.StudentId != studentEntity.Id)
        {
            stu.StudentId = studentEntity.Id;
            await userMgr.UpdateAsync(stu);
        }

        // Ensure a sample Course
        var course = db.Courses.FirstOrDefault(c => c.Code == "CS101");
        if (course == null)
        {
            course = new Course
            {
                Code = "CS101",
                Title = "Intro to Computer Science",
                Description = "Foundations of computing.",
                Credits = 3,
                Capacity = 50,
                RequiresApproval = false,
                DepartmentId = dept.Id,
                Schedule = "MWF 10:00-10:50"
            };
            db.Courses.Add(course);
            await db.SaveChangesAsync();
        }

        // Link instructor to course
        if (!db.CourseInstructors.Any(ci => ci.CourseId == course.Id && ci.InstructorId == instructorEntity.Id))
        {
            db.CourseInstructors.Add(new CourseInstructor
            {
                CourseId = course.Id,
                InstructorId = instructorEntity.Id
            });
            await db.SaveChangesAsync();
        }

        // Enroll student in course for current term
        var term = $"Fall {DateTime.UtcNow.Year}";
        if (!db.Enrollments.Any(e => e.StudentId == studentEntity.Id && e.CourseId == course.Id && e.Term == term))
        {
            db.Enrollments.Add(new Enrollment
            {
                StudentId = studentEntity.Id,
                CourseId = course.Id,
                Term = term,
                Status = EnrollmentStatus.Approved,
                NumericGrade = null
            });
            await db.SaveChangesAsync();
        }
    }
}
