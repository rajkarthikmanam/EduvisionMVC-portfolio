using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using EduvisionMvc.Data;
using EduvisionMvc.Models;

namespace EduvisionMvc.Controllers
{
    [Route("dev/[controller]")]
    [AllowAnonymous]
    public class SeedController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public SeedController(AppDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet("")]
        public async Task<IActionResult> Run()
        {
            try
            {
                // STEP 1: Clear ALL existing data (except admin user and roles)
                _db.Submissions.RemoveRange(_db.Submissions);
                _db.Assignments.RemoveRange(_db.Assignments);
                _db.DiscussionPosts.RemoveRange(_db.DiscussionPosts);
                _db.Discussions.RemoveRange(_db.Discussions);
                _db.Announcements.RemoveRange(_db.Announcements);
                _db.CourseMaterials.RemoveRange(_db.CourseMaterials);
                _db.Notifications.RemoveRange(_db.Notifications);
                _db.EnrollmentHistory.RemoveRange(_db.EnrollmentHistory);
                _db.Enrollments.RemoveRange(_db.Enrollments);
                _db.CourseInstructors.RemoveRange(_db.CourseInstructors);
                _db.Courses.RemoveRange(_db.Courses);
                _db.Students.RemoveRange(_db.Students);
                _db.Instructors.RemoveRange(_db.Instructors);
                _db.Departments.RemoveRange(_db.Departments);
                await _db.SaveChangesAsync();
                
                // Delete student and instructor users (keep admin)
                var usersToDelete = await _userManager.Users.Where(u => u.Email != "admin@local.test").ToListAsync();
                foreach (var user in usersToDelete)
                {
                    await _userManager.DeleteAsync(user);
                }

                // STEP 2: Create exactly 5 Departments
                var departments = new List<Department>
                {
                    new Department { Name = "Computer Science", Code = "CS", OfficeLocation = "Tech Building 201", Phone = "555-101-0001", Email = "cs@university.edu", Description = "Cutting-edge computer science and software engineering programs" },
                    new Department { Name = "Mathematics", Code = "MATH", OfficeLocation = "Science Hall 305", Phone = "555-101-0002", Email = "math@university.edu", Description = "Pure and applied mathematics department" },
                    new Department { Name = "Physics", Code = "PHYS", OfficeLocation = "Science Hall 102", Phone = "555-101-0003", Email = "physics@university.edu", Description = "Exploring the fundamental laws of nature" },
                    new Department { Name = "Business Administration", Code = "BUS", OfficeLocation = "Commerce Center 401", Phone = "555-101-0004", Email = "business@university.edu", Description = "Comprehensive business and management education" },
                    new Department { Name = "English Literature", Code = "ENG", OfficeLocation = "Arts Building 220", Phone = "555-101-0005", Email = "english@university.edu", Description = "Literary studies and creative writing programs" }
                };
                
                _db.Departments.AddRange(departments);
                await _db.SaveChangesAsync();

                // STEP 3: Create exactly 5 Instructors with complete data
                var instructorData = new[]
                {
                    new { FirstName = "Sarah", LastName = "Johnson", Email = "sarah.johnson@university.edu", DepartmentId = departments[0].Id, Title = "Professor", Office = "Tech 305", Phone = "555-201-0001" },
                    new { FirstName = "Michael", LastName = "Chen", Email = "michael.chen@university.edu", DepartmentId = departments[1].Id, Title = "Associate Professor", Office = "Science 410", Phone = "555-201-0002" },
                    new { FirstName = "Emily", LastName = "Rodriguez", Email = "emily.rodriguez@university.edu", DepartmentId = departments[2].Id, Title = "Assistant Professor", Office = "Science 215", Phone = "555-201-0003" },
                    new { FirstName = "David", LastName = "Thompson", Email = "david.thompson@university.edu", DepartmentId = departments[3].Id, Title = "Professor", Office = "Commerce 520", Phone = "555-201-0004" },
                    new { FirstName = "Jennifer", LastName = "Williams", Email = "jennifer.williams@university.edu", DepartmentId = departments[4].Id, Title = "Lecturer", Office = "Arts 310", Phone = "555-201-0005" }
                };

                var instructors = new List<Instructor>();
                foreach (var iData in instructorData)
                {
                    var user = new ApplicationUser
                    {
                        UserName = iData.Email,
                        Email = iData.Email,
                        EmailConfirmed = true,
                        FirstName = iData.FirstName,
                        LastName = iData.LastName,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    var result = await _userManager.CreateAsync(user, "Instructor123!");
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, "Instructor");
                        
                        var instructor = new Instructor
                        {
                            UserId = user.Id,
                            FirstName = iData.FirstName,
                            LastName = iData.LastName,
                            Email = iData.Email,
                            DepartmentId = iData.DepartmentId,
                            Title = iData.Title,
                            OfficeLocation = iData.Office,
                            Phone = iData.Phone,
                            OfficeHours = "MW 2-4pm, or by appointment",
                            HireDate = DateTime.Now.AddYears(-5),
                            Bio = $"Experienced {iData.Title.ToLower()} specializing in advanced topics."
                        };
                        
                        _db.Instructors.Add(instructor);
                        await _db.SaveChangesAsync();
                        
                        user.InstructorId = instructor.Id;
                        await _userManager.UpdateAsync(user);
                        
                        instructors.Add(instructor);
                    }
                }

                // STEP 4: Create exactly 5 Students with complete data
                var studentData = new[]
                {
                    new { FirstName = "Alex", LastName = "Martinez", Email = "alex.martinez@student.edu", DepartmentId = departments[0].Id, Level = "Junior", GPA = 3.75m, Phone = "555-301-0001" },
                    new { FirstName = "Jessica", LastName = "Taylor", Email = "jessica.taylor@student.edu", DepartmentId = departments[1].Id, Level = "Senior", GPA = 3.90m, Phone = "555-301-0002" },
                    new { FirstName = "Ryan", LastName = "Anderson", Email = "ryan.anderson@student.edu", DepartmentId = departments[2].Id, Level = "Sophomore", GPA = 3.50m, Phone = "555-301-0003" },
                    new { FirstName = "Sophia", LastName = "White", Email = "sophia.white@student.edu", DepartmentId = departments[3].Id, Level = "Senior", GPA = 3.85m, Phone = "555-301-0004" },
                    new { FirstName = "Daniel", LastName = "Brown", Email = "daniel.brown@student.edu", DepartmentId = departments[4].Id, Level = "Freshman", GPA = 3.60m, Phone = "555-301-0005" }
                };

                var students = new List<Student>();
                foreach (var sData in studentData)
                {
                    var user = new ApplicationUser
                    {
                        UserName = sData.Email,
                        Email = sData.Email,
                        EmailConfirmed = true,
                        FirstName = sData.FirstName,
                        LastName = sData.LastName,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    var result = await _userManager.CreateAsync(user, "Student123!");
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, "Student");
                        
                        var advisor = instructors.FirstOrDefault(i => i.DepartmentId == sData.DepartmentId);
                        var student = new Student
                        {
                            UserId = user.Id,
                            Name = $"{sData.FirstName} {sData.LastName}",
                            Email = sData.Email,
                            Major = departments.First(d => d.Id == sData.DepartmentId).Name,
                            DepartmentId = sData.DepartmentId,
                            AdvisorInstructorId = advisor?.Id,
                            Phone = sData.Phone,
                            AcademicLevel = sData.Level,
                            Gpa = sData.GPA,
                            TotalCredits = 0,
                            EnrollmentDate = DateTime.Now.AddYears(-2),
                            Age = 20
                        };
                        
                        _db.Students.Add(student);
                        await _db.SaveChangesAsync();
                        
                        user.StudentId = student.Id;
                        await _userManager.UpdateAsync(user);
                        
                        students.Add(student);
                    }
                }

                // STEP 5: Create exactly 5 Courses with complete data
                var courseData = new[]
                {
                    new { Code = "CS101", Title = "Introduction to Programming", DepartmentId = departments[0].Id, Credits = 3, Capacity = 30, Level = "100", InstructorId = instructors[0].Id },
                    new { Code = "MATH201", Title = "Calculus II", DepartmentId = departments[1].Id, Credits = 4, Capacity = 25, Level = "200", InstructorId = instructors[1].Id },
                    new { Code = "PHYS301", Title = "Quantum Mechanics", DepartmentId = departments[2].Id, Credits = 4, Capacity = 20, Level = "300", InstructorId = instructors[2].Id },
                    new { Code = "BUS250", Title = "Marketing Fundamentals", DepartmentId = departments[3].Id, Credits = 3, Capacity = 35, Level = "200", InstructorId = instructors[3].Id },
                    new { Code = "ENG150", Title = "Creative Writing Workshop", DepartmentId = departments[4].Id, Credits = 3, Capacity = 18, Level = "100", InstructorId = instructors[4].Id }
                };

                var courses = new List<Course>();
                foreach (var cData in courseData)
                {
                    var course = new Course
                    {
                        Code = cData.Code,
                        Title = cData.Title,
                        Description = $"Comprehensive {cData.Title.ToLower()} course covering fundamental concepts and practical applications.",
                        Credits = cData.Credits,
                        DepartmentId = cData.DepartmentId,
                        Capacity = cData.Capacity,
                        Level = cData.Level,
                        DeliveryMode = "In-Person",
                        Schedule = "MWF 10:00-10:50",
                        Location = "Building 1, Room 101",
                        StartDate = new DateTime(2025, 8, 25),
                        EndDate = new DateTime(2025, 12, 15),
                        Prerequisites = null
                    };
                    
                    _db.Courses.Add(course);
                    await _db.SaveChangesAsync();
                    
                    // Assign instructor to course
                    _db.CourseInstructors.Add(new CourseInstructor
                    {
                        CourseId = course.Id,
                        InstructorId = cData.InstructorId
                    });
                    
                    courses.Add(course);
                }
                
                await _db.SaveChangesAsync();

                // STEP 6: Create exactly 5 Enrollments with complete data (mix of current and completed)
                var enrollmentData = new[]
                {
                    new { StudentId = students[0].Id, CourseId = courses[0].Id, Term = "Fall 2025", Grade = (decimal?)3.7m, Status = EnrollmentStatus.Approved },
                    new { StudentId = students[1].Id, CourseId = courses[1].Id, Term = "Fall 2025", Grade = (decimal?)4.0m, Status = EnrollmentStatus.Approved },
                    new { StudentId = students[2].Id, CourseId = courses[2].Id, Term = "Fall 2025", Grade = (decimal?)null, Status = EnrollmentStatus.Approved }, // Current enrollment
                    new { StudentId = students[3].Id, CourseId = courses[3].Id, Term = "Fall 2025", Grade = (decimal?)null, Status = EnrollmentStatus.Approved }, // Current enrollment
                    new { StudentId = students[4].Id, CourseId = courses[4].Id, Term = "Fall 2025", Grade = (decimal?)3.5m, Status = EnrollmentStatus.Approved }
                };

                foreach (var eData in enrollmentData)
                {
                    var enrollment = new Enrollment
                    {
                        StudentId = eData.StudentId,
                        CourseId = eData.CourseId,
                        Term = eData.Term,
                        EnrolledDate = DateTime.Now.AddMonths(-3),
                        Status = eData.Status,
                        Numeric_Grade = eData.Grade,
                        LetterGrade = eData.Grade.HasValue ? NumericToLetter(eData.Grade.Value) : null,
                        ProgressPercentage = eData.Grade.HasValue ? 100 : 65,
                        TotalHoursSpent = eData.Grade.HasValue ? 45 : 30
                    };
                    
                    _db.Enrollments.Add(enrollment);
                }
                
                await _db.SaveChangesAsync();

                // Update student total credits for completed courses
                foreach (var student in students)
                {
                    var completedCredits = _db.Enrollments
                        .Include(e => e.Course)
                        .Where(e => e.StudentId == student.Id && e.Numeric_Grade.HasValue)
                        .Sum(e => e.Course!.Credits);
                    student.TotalCredits = completedCredits;
                }
                
                await _db.SaveChangesAsync();

                var summary = new
                {
                    Status = "Success",
                    Message = "Database seeded with exactly 5 rows per table",
                    Departments = departments.Count,
                    Instructors = instructors.Count,
                    Students = students.Count,
                    Courses = courses.Count,
                    Enrollments = enrollmentData.Length,
                    Details = "All data includes complete information with proper relationships",
                    Hint = "Use /dev/seed/import to load provided_seed.json without wiping existing data."
                };

                return Json(summary);
            }
            catch (Exception ex)
            {
                return Json(new { Status = "Error", Message = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        // New endpoint: Import from provided_seed.json WITHOUT wiping existing data, idempotent insert/update
        [HttpGet("import")]
        public async Task<IActionResult> ImportProvided()
        {
            try
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "provided_seed.json");
                if (!System.IO.File.Exists(path))
                {
                    return Json(new { Status = "Error", Message = "provided_seed.json not found in App_Data" });
                }

                var json = await System.IO.File.ReadAllTextAsync(path);
                var doc = System.Text.Json.JsonDocument.Parse(json);

                int addedDepartments = 0, addedInstructors = 0, addedCourses = 0, addedStudents = 0, addedEnrollments = 0;
                var depMap = new Dictionary<int, int>();
                var instMap = new Dictionary<int, int>();
                var courseMap = new Dictionary<int, int>();
                var studentMap = new Dictionary<int, int>();

                // Departments (match on Code)
                if (doc.RootElement.TryGetProperty("Departments", out var departmentsEl))
                {
                    foreach (var dep in departmentsEl.EnumerateArray())
                    {
                        var jsonDepId = dep.TryGetProperty("Id", out var depIdProp) && depIdProp.ValueKind == System.Text.Json.JsonValueKind.Number ? depIdProp.GetInt32() : 0;
                        var code = dep.GetProperty("Code").GetString();
                        if (string.IsNullOrWhiteSpace(code)) continue;
                        var dbDept = _db.Departments.FirstOrDefault(d => d.Code == code);
                        if (dbDept == null)
                        {
                            dbDept = new Department
                            {
                                Code = code!,
                                Name = dep.GetProperty("Name").GetString() ?? code!,
                                Description = dep.GetProperty("Description").GetString(),
                                OfficeLocation = dep.GetProperty("OfficeLocation").GetString(),
                                Phone = dep.GetProperty("Phone").GetString(),
                                Email = dep.GetProperty("Email").GetString()
                            };
                            _db.Departments.Add(dbDept);
                            addedDepartments++;
                            await _db.SaveChangesAsync();
                        }
                        if (jsonDepId != 0) depMap[jsonDepId] = dbDept.Id;
                    }
                }

                // Instructors (match on Email)
                if (doc.RootElement.TryGetProperty("Instructors", out var instructorsEl))
                {
                    foreach (var ins in instructorsEl.EnumerateArray())
                    {
                        var jsonInstId = ins.TryGetProperty("Id", out var idProp) && idProp.ValueKind == System.Text.Json.JsonValueKind.Number ? idProp.GetInt32() : 0;
                        var email = ins.GetProperty("Email").GetString();
                        if (string.IsNullOrWhiteSpace(email)) continue;
                        var dbInstr = _db.Instructors.FirstOrDefault(i => i.Email == email);
                        if (dbInstr == null)
                        {
                            var jsonDeptId = ins.GetProperty("DepartmentId").GetInt32();
                            var mappedDeptId = depMap.TryGetValue(jsonDeptId, out var dId) ? dId : jsonDeptId;
                            dbInstr = new Instructor
                            {
                                FirstName = ins.GetProperty("FirstName").GetString() ?? "Unknown",
                                LastName = ins.GetProperty("LastName").GetString() ?? "Unknown",
                                Email = email!,
                                DepartmentId = mappedDeptId,
                                Title = ins.GetProperty("Title").GetString(),
                                HireDate = DateTime.TryParse(ins.GetProperty("HireDate").GetString(), out var hd) ? hd : DateTime.UtcNow.AddYears(-1)
                            };
                            _db.Instructors.Add(dbInstr);
                            addedInstructors++;
                            await _db.SaveChangesAsync();
                        }
                        if (jsonInstId != 0) instMap[jsonInstId] = dbInstr.Id;
                    }
                }

                // Courses (match on Code). Also create CourseInstructor mapping from InstructorId
                if (doc.RootElement.TryGetProperty("Courses", out var coursesEl))
                {
                    foreach (var c in coursesEl.EnumerateArray())
                    {
                        var jsonCourseId = c.TryGetProperty("Id", out var idProp) && idProp.ValueKind == System.Text.Json.JsonValueKind.Number ? idProp.GetInt32() : 0;
                        var code = c.GetProperty("Code").GetString();
                        if (string.IsNullOrWhiteSpace(code)) continue;
                        var dbCourse = _db.Courses.FirstOrDefault(x => x.Code == code);
                        if (dbCourse == null)
                        {
                            var mappedDeptId = depMap.TryGetValue(c.GetProperty("DepartmentId").GetInt32(), out var dId) ? dId : c.GetProperty("DepartmentId").GetInt32();
                            dbCourse = new Course
                            {
                                Code = code!,
                                Title = c.GetProperty("Title").GetString() ?? code!,
                                Description = c.GetProperty("Description").GetString() ?? string.Empty,
                                Credits = c.GetProperty("Credits").GetInt32(),
                                Capacity = c.GetProperty("Capacity").GetInt32(),
                                RequiresApproval = c.GetProperty("RequiresApproval").GetBoolean(),
                                DepartmentId = mappedDeptId,
                                StartDate = DateTime.TryParse(c.GetProperty("StartDate").GetString(), out var sd) ? sd : (DateTime?)null,
                                EndDate = DateTime.TryParse(c.GetProperty("EndDate").GetString(), out var ed) ? ed : (DateTime?)null
                            };
                            _db.Courses.Add(dbCourse);
                            await _db.SaveChangesAsync();
                            addedCourses++;
                        }
                        if (jsonCourseId != 0) courseMap[jsonCourseId] = dbCourse.Id;

                        if (c.TryGetProperty("InstructorId", out var instProp) && instProp.ValueKind == System.Text.Json.JsonValueKind.Number)
                        {
                            var jsonInstId = instProp.GetInt32();
                            if (instMap.TryGetValue(jsonInstId, out var dbInstId))
                            {
                                if (!_db.CourseInstructors.Any(ci => ci.CourseId == dbCourse.Id && ci.InstructorId == dbInstId))
                                {
                                    _db.CourseInstructors.Add(new CourseInstructor { CourseId = dbCourse.Id, InstructorId = dbInstId });
                                }
                            }
                        }
                    }
                    await _db.SaveChangesAsync();
                }

                // Students (match on Email)
                if (doc.RootElement.TryGetProperty("Students", out var studentsEl))
                {
                    foreach (var s in studentsEl.EnumerateArray())
                    {
                        var jsonStudentId = s.TryGetProperty("Id", out var sidProp) && sidProp.ValueKind == System.Text.Json.JsonValueKind.Number ? sidProp.GetInt32() : 0;
                        var email = s.GetProperty("Email").GetString();
                        if (string.IsNullOrWhiteSpace(email)) continue;
                        var dbStudent = _db.Students.FirstOrDefault(st => st.Email == email);
                        if (dbStudent == null)
                        {
                            var mappedDeptId = depMap.TryGetValue(s.GetProperty("DepartmentId").GetInt32(), out var dId) ? dId : s.GetProperty("DepartmentId").GetInt32();
                            dbStudent = new Student
                            {
                                Name = s.GetProperty("Name").GetString() ?? "Unknown",
                                Email = email!,
                                Major = s.GetProperty("Major").GetString() ?? string.Empty,
                                DepartmentId = mappedDeptId,
                                EnrollmentDate = DateTime.TryParse(s.GetProperty("EnrollmentDate").GetString(), out var ed) ? ed : DateTime.UtcNow.AddMonths(-1),
                                Gpa = s.TryGetProperty("Gpa", out var gpaProp) && gpaProp.ValueKind == System.Text.Json.JsonValueKind.Number ? gpaProp.GetDecimal() : 0m,
                                TotalCredits = s.TryGetProperty("TotalCredits", out var tcProp) && tcProp.ValueKind == System.Text.Json.JsonValueKind.Number ? tcProp.GetInt32() : 0,
                                Age = 20
                            };
                            _db.Students.Add(dbStudent);
                            addedStudents++;
                            await _db.SaveChangesAsync();
                        }
                        if (jsonStudentId != 0) studentMap[jsonStudentId] = dbStudent.Id;
                    }
                }

                // Enrollments (match on StudentId+CourseId+Term) - only add if both student & course exist
                if (doc.RootElement.TryGetProperty("Enrollments", out var enrollmentsEl))
                {
                    foreach (var e in enrollmentsEl.EnumerateArray())
                    {
                        var jsonStudentId = e.GetProperty("StudentId").GetInt32();
                        var jsonCourseId = e.GetProperty("CourseId").GetInt32();
                        var term = e.GetProperty("Term").GetString() ?? "Unknown";
                        if (!studentMap.TryGetValue(jsonStudentId, out var studentId)) continue;
                        if (!courseMap.TryGetValue(jsonCourseId, out var courseId)) continue;
                        if (_db.Enrollments.Any(en => en.StudentId == studentId && en.CourseId == courseId && en.Term == term)) continue;

                        var statusStr = e.GetProperty("Status").GetString();
                        EnrollmentStatus status = statusStr switch
                        {
                            "Approved" => EnrollmentStatus.Approved,
                            "Pending" => EnrollmentStatus.Pending,
                            "Rejected" => EnrollmentStatus.Rejected,
                            "Dropped" => EnrollmentStatus.Dropped,
                            "Completed" => EnrollmentStatus.Completed,
                            _ => EnrollmentStatus.Pending
                        };

                        decimal? numericGrade = null;
                        if (e.TryGetProperty("Numeric_Grade", out var numGradeProp) && numGradeProp.ValueKind == System.Text.Json.JsonValueKind.Number)
                        {
                            numericGrade = numGradeProp.GetDecimal();
                            if (numericGrade > 10m)
                            {
                                numericGrade = Math.Round(numericGrade.Value / 25m, 2);
                            }
                        }
                        string? letterGrade = null;
                        if (e.TryGetProperty("LetterGrade", out var letterGradeProp) && letterGradeProp.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            letterGrade = letterGradeProp.GetString();
                        }

                        var enrollment = new Enrollment
                        {
                            StudentId = studentId,
                            CourseId = courseId,
                            Term = term,
                            Status = status,
                            Numeric_Grade = numericGrade,
                            LetterGrade = letterGrade,
                            EnrolledDate = DateTime.TryParse(e.GetProperty("EnrolledDate").GetString(), out var enrollDate) ? enrollDate : DateTime.UtcNow.AddDays(-7),
                            ApprovedDate = e.TryGetProperty("ApprovedDate", out var appDateProp) && DateTime.TryParse(appDateProp.GetString(), out var appDate) ? appDate : null,
                            ProgressPercentage = numericGrade.HasValue ? 100 : 50,
                            TotalHoursSpent = numericGrade.HasValue ? 40 : 10
                        };
                        _db.Enrollments.Add(enrollment);
                        addedEnrollments++;
                    }
                    await _db.SaveChangesAsync();
                }

                // Recalculate TotalCredits for any students affected (credits from enrollments with numeric grade)
                var studentsToUpdate = _db.Students.ToList();
                foreach (var st in studentsToUpdate)
                {
                    var credits = _db.Enrollments.Include(x => x.Course).Where(x => x.StudentId == st.Id && x.Numeric_Grade.HasValue).Sum(x => x.Course!.Credits);
                    st.TotalCredits = credits;
                }
                await _db.SaveChangesAsync();

                return Json(new
                {
                    Status = "Success",
                    Message = "Import completed (idempotent). Existing data preserved.",
                    Added = new
                    {
                        Departments = addedDepartments,
                        Instructors = addedInstructors,
                        Courses = addedCourses,
                        Students = addedStudents,
                        Enrollments = addedEnrollments
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { Status = "Error", Message = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        private static string NumericToLetter(decimal grade)
        {
            if (grade >= 3.7m) return "A";
            if (grade >= 3.0m) return "B";
            if (grade >= 2.0m) return "C";
            if (grade >= 1.0m) return "D";
            return "F";
        }

        // Status endpoint: returns counts for quick verification without mutating data
        [HttpGet("status")]
        public IActionResult Status()
        {
            var counts = new
            {
                Departments = _db.Departments.Count(),
                Instructors = _db.Instructors.Count(),
                Courses = _db.Courses.Count(),
                CourseInstructors = _db.CourseInstructors.Count(),
                Students = _db.Students.Count(),
                Enrollments = _db.Enrollments.Count()
            };
            return Json(new { Status = "OK", counts });
        }
    }
}
