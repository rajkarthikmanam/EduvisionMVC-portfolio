using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using EduvisionMvc.Models;

namespace EduvisionMvc.Data;

public class AppDbContext(DbContextOptions<AppDbContext> opts) : IdentityDbContext<ApplicationUser>(opts)
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<CourseInstructor> CourseInstructors => Set<CourseInstructor>();
    public DbSet<CourseMaterial> CourseMaterials => Set<CourseMaterial>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<EnrollmentHistory> EnrollmentHistory => Set<EnrollmentHistory>();
        public DbSet<Assignment> Assignments => Set<Assignment>();
        public DbSet<AssignmentSubmission> Submissions => Set<AssignmentSubmission>();
        public DbSet<Discussion> Discussions => Set<Discussion>();
        public DbSet<DiscussionPost> DiscussionPosts => Set<DiscussionPost>();
        public DbSet<CourseAnnouncement> Announcements => Set<CourseAnnouncement>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Configure Identity tables
        b.Entity<ApplicationUser>()
            .Property(u => u.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Configure Student
        b.Entity<Student>()
            .Property(x => x.Gpa)
            .HasPrecision(3, 2);

        // Unique Email for Student test identity mapping
        b.Entity<Student>()
            .HasIndex(s => s.Email)
            .IsUnique();

        b.Entity<Student>()
            .HasOne(s => s.User)
            .WithOne(u => u.Student)
            .HasForeignKey<ApplicationUser>(u => u.StudentId);

        // Configure Instructor
        b.Entity<Instructor>()
            .HasOne(i => i.User)
            .WithOne(u => u.Instructor)
            .HasForeignKey<ApplicationUser>(u => u.InstructorId);

        // Unique Email for Instructor test identity mapping
        b.Entity<Instructor>()
            .HasIndex(i => i.Email)
            .IsUnique();

        // Link Student to Department (optional FK)
        b.Entity<Student>()
            .HasOne(s => s.Department)
            .WithMany(d => d.Students)
            .HasForeignKey(s => s.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        // Student advisor (optional link to Instructor)
        b.Entity<Student>()
            .HasOne(s => s.AdvisorInstructor)
            .WithMany()
            .HasForeignKey(s => s.AdvisorInstructorId)
            .OnDelete(DeleteBehavior.SetNull);

        // Department chair (optional)
        b.Entity<Department>()
            .HasOne(d => d.Chair)
            .WithMany()
            .HasForeignKey(d => d.ChairId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure Enrollment relationships
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

        // Unique constraint: One student per course per term
        b.Entity<Enrollment>()
            .HasIndex(e => new { e.StudentId, e.CourseId, e.Term })
            .IsUnique();

        // Configure Course relationships
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

        // Course code must be unique within a department
        b.Entity<Course>()
            .HasIndex(c => new { c.Code, c.DepartmentId })
            .IsUnique();

        // Configure EnrollmentHistory tracking
        b.Entity<EnrollmentHistory>()
            .HasOne(h => h.Enrollment)
            .WithMany(e => e.History)
            .HasForeignKey(h => h.EnrollmentId);

        b.Entity<EnrollmentHistory>()
            .HasOne(h => h.UpdatedBy)
            .WithMany(u => u.EnrollmentChanges)
            .HasForeignKey(h => h.UpdatedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure Course Materials
        b.Entity<CourseMaterial>()
            .HasOne(m => m.Course)
            .WithMany(c => c.Materials)
            .HasForeignKey(m => m.CourseId);

        b.Entity<CourseMaterial>()
            .HasOne(m => m.UploadedBy)
            .WithMany(u => u.UploadedMaterials)
            .HasForeignKey(m => m.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure Notifications
        b.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId);

            // Configure Assignment relationships
            b.Entity<Assignment>()
                .HasOne(a => a.Course)
                .WithMany(c => c.Assignments)
                .HasForeignKey(a => a.CourseId);

            b.Entity<AssignmentSubmission>()
                .HasOne(s => s.Assignment)
                .WithMany(a => a.Submissions)
                .HasForeignKey(s => s.AssignmentId);

            b.Entity<AssignmentSubmission>()
                .HasOne(s => s.Student)
                .WithMany(s => s.Submissions)
                .HasForeignKey(s => s.StudentId);

            // Configure Discussion relationships
            b.Entity<Discussion>()
                .HasOne(d => d.Course)
                .WithMany(c => c.Discussions)
                .HasForeignKey(d => d.CourseId);

            b.Entity<DiscussionPost>()
                .HasOne(p => p.Discussion)
                .WithMany(d => d.Posts)
                .HasForeignKey(p => p.DiscussionId);

            b.Entity<DiscussionPost>()
                .HasOne(p => p.Author)
                .WithMany(u => u.DiscussionPosts)
                .HasForeignKey(p => p.AuthorId);

            b.Entity<CourseAnnouncement>()
                .HasOne(a => a.Course)
                .WithMany(c => c.Announcements)
                .HasForeignKey(a => a.CourseId);

            b.Entity<CourseAnnouncement>()
                .HasOne(a => a.Author)
                .WithMany(u => u.CourseAnnouncements)
                .HasForeignKey(a => a.AuthorId);

    }
}
