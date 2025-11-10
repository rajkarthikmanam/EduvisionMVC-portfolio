using EduvisionMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace EduvisionMvc.Data;

public static class LmsDataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // Check if we already have enough data
        if (await db.Students.CountAsync() > 5) return;

        // This will be called AFTER IdentitySeeder, so departments and instructors exist
        await SeedComprehensiveDataAsync(db);
    }

    private static async Task SeedComprehensiveDataAsync(AppDbContext db)
    {
        var currentTerm = "Fall 2025";
        var previousTerm = "Spring 2025";
        
        // Get departments
        var csDept = await db.Departments.FirstOrDefaultAsync(d => d.Code == "CS");
        var mathDept = await db.Departments.FirstOrDefaultAsync(d => d.Code == "MATH");
        var engDept = await db.Departments.FirstOrDefaultAsync(d => d.Code == "ENG");
        var busDept = await db.Departments.FirstOrDefaultAsync(d => d.Code == "BUS");
        
        if (csDept == null || mathDept == null || engDept == null || busDept == null)
            return; // Departments not yet created by IdentitySeeder
        
        // Get instructors
        var csInst = await db.Instructors.FirstOrDefaultAsync(i => i.Email == "instructor@local.test");
        var mathInst = await db.Instructors.FirstOrDefaultAsync(i => i.Email == "math.prof@local.test");
        var engInst = await db.Instructors.FirstOrDefaultAsync(i => i.Email == "eng.prof@local.test");
        var busInst = await db.Instructors.FirstOrDefaultAsync(i => i.Email == "bus.prof@local.test");
        
        // Add more courses
        await SeedCourses(db, csDept, mathDept, engDept, busDept);
        
        // Link instructors to courses
        await LinkInstructorsToCourses(db, csInst, mathInst, engInst, busInst, csDept, mathDept, engDept, busDept);
        
        // Create 12 students
        await SeedStudents(db);
        
        // Create enrollments
        await SeedEnrollments(db, currentTerm, previousTerm);
    }

    private static async Task SeedCourses(AppDbContext db, Department cs, Department math, Department eng, Department bus)
    {
        var courses = new List<Course>();
        
        // CS courses
        if (!await db.Courses.AnyAsync(c => c.Code == "CS201"))
            courses.Add(new Course { Code = "CS201", Title = "Data Structures", Description = "Advanced DS", Credits = 4, Capacity = 40, DepartmentId = cs.Id, Schedule = "TTh 09:00-10:45" });
        if (!await db.Courses.AnyAsync(c => c.Code == "CS250"))
            courses.Add(new Course { Code = "CS250", Title = "Web Development", Description = "Full-stack", Credits = 3, Capacity = 35, DepartmentId = cs.Id, Schedule = "MW 14:00-15:15" });
        if (!await db.Courses.AnyAsync(c => c.Code == "CS310"))
            courses.Add(new Course { Code = "CS310", Title = "Databases", Description = "SQL and more", Credits = 3, Capacity = 45, DepartmentId = cs.Id, Schedule = "TTh 11:00-12:15" });
            
        // Math courses
        if (!await db.Courses.AnyAsync(c => c.Code == "MATH201"))
            courses.Add(new Course { Code = "MATH201", Title = "Calculus II", Description = "Integration", Credits = 4, Capacity = 50, DepartmentId = math.Id, Schedule = "MWF 10:00-10:50" });
        if (!await db.Courses.AnyAsync(c => c.Code == "MATH301"))
            courses.Add(new Course { Code = "MATH301", Title = "Linear Algebra", Description = "Vectors", Credits = 3, Capacity = 40, DepartmentId = math.Id, Schedule = "TTh 13:00-14:15" });
        if (!await db.Courses.AnyAsync(c => c.Code == "MATH401"))
            courses.Add(new Course { Code = "MATH401", Title = "Statistics", Description = "Probability", Credits = 3, Capacity = 35, DepartmentId = math.Id, Schedule = "MW 11:00-12:15" });
            
        // Eng courses
        if (!await db.Courses.AnyAsync(c => c.Code == "ENG101"))
            courses.Add(new Course { Code = "ENG101", Title = "Intro Engineering", Description = "Fundamentals", Credits = 3, Capacity = 60, DepartmentId = eng.Id, Schedule = "MW 09:00-10:15" });
        if (!await db.Courses.AnyAsync(c => c.Code == "ENG250"))
            courses.Add(new Course { Code = "ENG250", Title = "Thermodynamics", Description = "Heat systems", Credits = 4, Capacity = 35, DepartmentId = eng.Id, Schedule = "TTh 14:30-16:15" });
        if (!await db.Courses.AnyAsync(c => c.Code == "ENG310"))
            courses.Add(new Course { Code = "ENG310", Title = "Circuits", Description = "Electronics", Credits = 4, Capacity = 30, DepartmentId = eng.Id, Schedule = "MWF 13:00-14:15" });
            
        // Bus courses
        if (!await db.Courses.AnyAsync(c => c.Code == "BUS101"))
            courses.Add(new Course { Code = "BUS101", Title = "Business Fund", Description = "Intro business", Credits = 3, Capacity = 80, DepartmentId = bus.Id, Schedule = "MW 15:30-16:45" });
        if (!await db.Courses.AnyAsync(c => c.Code == "BUS210"))
            courses.Add(new Course { Code = "BUS210", Title = "Accounting", Description = "Financial", Credits = 3, Capacity = 50, DepartmentId = bus.Id, Schedule = "TTh 10:30-11:45" });
        if (!await db.Courses.AnyAsync(c => c.Code == "BUS305"))
            courses.Add(new Course { Code = "BUS305", Title = "Marketing", Description = "Strategies", Credits = 3, Capacity = 45, DepartmentId = bus.Id, Schedule = "MW 16:00-17:15" });
        
        db.Courses.AddRange(courses);
        await db.SaveChangesAsync();
    }

    private static async Task LinkInstructorsToCourses(AppDbContext db, Instructor? csInst, Instructor? mathInst, Instructor? engInst, Instructor? busInst,
        Department csDept, Department mathDept, Department engDept, Department busDept)
    {
        var courses = await db.Courses.ToListAsync();
        
        if (csInst != null)
        {
            foreach (var course in courses.Where(c => c.DepartmentId == csDept.Id))
            {
                if (!await db.CourseInstructors.AnyAsync(ci => ci.CourseId == course.Id && ci.InstructorId == csInst.Id))
                    db.CourseInstructors.Add(new CourseInstructor { CourseId = course.Id, InstructorId = csInst.Id });
            }
        }
        
        if (mathInst != null)
        {
            foreach (var course in courses.Where(c => c.DepartmentId == mathDept.Id))
            {
                if (!await db.CourseInstructors.AnyAsync(ci => ci.CourseId == course.Id && ci.InstructorId == mathInst.Id))
                    db.CourseInstructors.Add(new CourseInstructor { CourseId = course.Id, InstructorId = mathInst.Id });
            }
        }
        
        if (engInst != null)
        {
            foreach (var course in courses.Where(c => c.DepartmentId == engDept.Id))
            {
                if (!await db.CourseInstructors.AnyAsync(ci => ci.CourseId == course.Id && ci.InstructorId == engInst.Id))
                    db.CourseInstructors.Add(new CourseInstructor { CourseId = course.Id, InstructorId = engInst.Id });
            }
        }
        
        if (busInst != null)
        {
            foreach (var course in courses.Where(c => c.DepartmentId == busDept.Id))
            {
                if (!await db.CourseInstructors.AnyAsync(ci => ci.CourseId == course.Id && ci.InstructorId == busInst.Id))
                    db.CourseInstructors.Add(new CourseInstructor { CourseId = course.Id, InstructorId = busInst.Id });
            }
        }
        
        await db.SaveChangesAsync();
    }

    private static async Task SeedStudents(AppDbContext db)
    {
        var students = new[]
        {
            new Student { Name = "Alice Williams", Email = "alice.w@students.edu", Major = "CS", Gpa = 3.8m, Age = 20, EnrollmentDate = DateTime.UtcNow.AddYears(-2), TotalCredits = 60 },
            new Student { Name = "Bob Martinez", Email = "bob.m@students.edu", Major = "MATH", Gpa = 3.5m, Age = 21, EnrollmentDate = DateTime.UtcNow.AddYears(-2), TotalCredits = 55 },
            new Student { Name = "Carol Davis", Email = "carol.d@students.edu", Major = "CS", Gpa = 3.9m, Age = 19, EnrollmentDate = DateTime.UtcNow.AddYears(-1), TotalCredits = 45 },
            new Student { Name = "David Lee", Email = "david.l@students.edu", Major = "ENG", Gpa = 3.2m, Age = 22, EnrollmentDate = DateTime.UtcNow.AddYears(-3), TotalCredits = 75 },
            new Student { Name = "Emma Brown", Email = "emma.b@students.edu", Major = "BUS", Gpa = 3.7m, Age = 20, EnrollmentDate = DateTime.UtcNow.AddYears(-2), TotalCredits = 58 },
            new Student { Name = "Frank Wilson", Email = "frank.w@students.edu", Major = "MATH", Gpa = 3.4m, Age = 21, EnrollmentDate = DateTime.UtcNow.AddYears(-2), TotalCredits = 52 },
            new Student { Name = "Grace Taylor", Email = "grace.t@students.edu", Major = "ENG", Gpa = 3.6m, Age = 20, EnrollmentDate = DateTime.UtcNow.AddYears(-2), TotalCredits = 61 },
            new Student { Name = "Henry Anderson", Email = "henry.a@students.edu", Major = "BUS", Gpa = 3.3m, Age = 22, EnrollmentDate = DateTime.UtcNow.AddYears(-3), TotalCredits = 70 },
            new Student { Name = "Isabel Thomas", Email = "isabel.t@students.edu", Major = "CS", Gpa = 3.85m, Age = 19, EnrollmentDate = DateTime.UtcNow.AddYears(-1), TotalCredits = 42 },
            new Student { Name = "Jack Moore", Email = "jack.m@students.edu", Major = "MATH", Gpa = 3.1m, Age = 21, EnrollmentDate = DateTime.UtcNow.AddYears(-2), TotalCredits = 48 },
            new Student { Name = "Karen White", Email = "karen.w@students.edu", Major = "ENG", Gpa = 3.65m, Age = 20, EnrollmentDate = DateTime.UtcNow.AddYears(-2), TotalCredits = 56 },
            new Student { Name = "Leo Harris", Email = "leo.h@students.edu", Major = "BUS", Gpa = 3.45m, Age = 21, EnrollmentDate = DateTime.UtcNow.AddYears(-2), TotalCredits = 54 }
        };
        
        foreach (var student in students)
        {
            if (!await db.Students.AnyAsync(s => s.Email == student.Email))
                db.Students.Add(student);
        }
        
        await db.SaveChangesAsync();
    }

    private static async Task SeedEnrollments(AppDbContext db, string currentTerm, string previousTerm)
    {
        var students = await db.Students.ToListAsync();
        var courses = await db.Courses.Include(c => c.Department).ToListAsync();
        var random = new Random(42);
        var grades = new[] { 4.0m, 3.7m, 3.3m, 3.0m, 2.7m, 3.5m, 3.8m, 3.2m };
        
        foreach (var student in students)
        {
            var majorCourses = courses.Where(c => c.Department != null && c.Department.Code == student.Major).ToList();
            
            // 3-5 completed courses
            foreach (var course in majorCourses.Take(random.Next(3, 6)))
            {
                if (!await db.Enrollments.AnyAsync(e => e.StudentId == student.Id && e.CourseId == course.Id))
                {
                    var grade = grades[random.Next(grades.Length)];
                    db.Enrollments.Add(new Enrollment
                    {
                        StudentId = student.Id,
                        CourseId = course.Id,
                        Term = previousTerm,
                        Status = EnrollmentStatus.Approved,
                        EnrolledDate = DateTime.UtcNow.AddDays(-120),
                        CompletedDate = DateTime.UtcNow.AddDays(-60),
                        Numeric_Grade = grade,
                        LetterGrade = GetLetterGrade(grade),
                        ProgressPercentage = 100,
                        TotalHoursSpent = random.Next(30, 60)
                    });
                }
            }
            
            // 3-4 current courses
            foreach (var course in majorCourses.Skip(3).Take(random.Next(3, 5)))
            {
                if (!await db.Enrollments.AnyAsync(e => e.StudentId == student.Id && e.CourseId == course.Id))
                {
                    db.Enrollments.Add(new Enrollment
                    {
                        StudentId = student.Id,
                        CourseId = course.Id,
                        Term = currentTerm,
                        Status = EnrollmentStatus.Approved,
                        EnrolledDate = DateTime.UtcNow.AddDays(-45),
                        ProgressPercentage = random.Next(20, 95),
                        LastAccessDate = DateTime.UtcNow.AddHours(-random.Next(1, 72)),
                        TotalHoursSpent = random.Next(10, 40)
                    });
                }
            }
        }
        
        await db.SaveChangesAsync();

        // --- Enrich courses with materials, assignments, announcements, discussions ---
        var users = await db.Set<ApplicationUser>().ToListAsync();
        var instructorUsers = users.Where(u => u.InstructorId != null).ToList();
        var studentUsers = users.Where(u => u.StudentId != null).ToList();

        foreach (var course in courses)
        {
            // Materials
            if (!await db.CourseMaterials.AnyAsync(m => m.CourseId == course.Id))
            {
                db.CourseMaterials.AddRange(new [] {
                    NewMaterial(course.Id, instructorUsers.FirstOrDefault()?.Id ?? "", "Syllabus", "Course overview", MaterialType.Document),
                    NewMaterial(course.Id, instructorUsers.FirstOrDefault()?.Id ?? "", "Intro Lecture", "Week 1 video", MaterialType.Video),
                    NewMaterial(course.Id, instructorUsers.FirstOrDefault()?.Id ?? "", "Reference Link", "External resource", MaterialType.Link)
                });
            }

            // Assignments
            if (!await db.Assignments.AnyAsync(a => a.CourseId == course.Id))
            {
                var assign1 = new Assignment { CourseId = course.Id, Title = $"{course.Code} HW1", Description = "Core concepts", DueDate = DateTime.UtcNow.AddDays(7) };
                var assign2 = new Assignment { CourseId = course.Id, Title = $"{course.Code} Project", Description = "Applied project", DueDate = DateTime.UtcNow.AddDays(21) };
                db.Assignments.AddRange(assign1, assign2);
            }

            // Announcements
            if (!await db.Announcements.AnyAsync(a => a.CourseId == course.Id))
            {
                db.Announcements.Add(new CourseAnnouncement {
                    CourseId = course.Id,
                    AuthorId = instructorUsers.FirstOrDefault()?.Id ?? "",
                    Title = "Welcome",
                    Content = $"Welcome to {course.Title}!",
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                });
            }

            // Discussion + sample posts
            if (!await db.Discussions.AnyAsync(d => d.CourseId == course.Id))
            {
                var disc = new Discussion { CourseId = course.Id, Title = $"{course.Code} General Q&A" };
                db.Discussions.Add(disc);
                await db.SaveChangesAsync();

                var firstStudent = studentUsers.FirstOrDefault();
                if (firstStudent != null)
                {
                    db.DiscussionPosts.Add(new DiscussionPost {
                        DiscussionId = disc.Id,
                        AuthorId = firstStudent.Id,
                        Content = "Excited to start!",
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    });
                }
            }
        }

        await db.SaveChangesAsync();
    }

    private static CourseMaterial NewMaterial(int courseId, string uploaderId, string title, string desc, MaterialType type) =>
        new CourseMaterial {
            CourseId = courseId,
            UploadedById = uploaderId,
            Title = title,
            Description = desc,
            Type = type,
            UploadedDate = DateTime.UtcNow.AddDays(-1)
        };

    private static string GetLetterGrade(decimal grade)
    {
        if (grade >= 3.7m) return "A";
        if (grade >= 3.3m) return "A-";
        if (grade >= 3.0m) return "B+";
        if (grade >= 2.7m) return "B";
        return "B-";
    }
}
