using Microsoft.AspNetCore.Identity;

namespace EduvisionMvc.Models;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateJoined { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Avatar { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginDate { get; set; }

    // Navigation property for additional profile data
    public UserProfile? Profile { get; set; }

    // Calculated full name
    public string FullName => $"{FirstName} {LastName}".Trim();
    
    // Optional links back to Student/Instructor records (set by AppDbContext foreign key)
    public int? StudentId { get; set; }
    public Student? Student { get; set; }
    
    public int? InstructorId { get; set; }
    public Instructor? Instructor { get; set; }
    
    // Navigation collections referenced by OnModelCreating
    public ICollection<CourseMaterial> UploadedMaterials { get; set; } = new List<CourseMaterial>();
    public ICollection<EnrollmentHistory> EnrollmentChanges { get; set; } = new List<EnrollmentHistory>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<DiscussionPost> DiscussionPosts { get; set; } = new List<DiscussionPost>();
    public ICollection<CourseAnnouncement> CourseAnnouncements { get; set; } = new List<CourseAnnouncement>();
    public ICollection<AssignmentSubmission> Submissions { get; set; } = new List<AssignmentSubmission>();
}