namespace EduvisionMvc.Models;

public class UserProfile
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;  // Foreign key to ApplicationUser
    public string? Bio { get; set; }
    public string? Location { get; set; }
    public string? Website { get; set; }
    public string? Skills { get; set; }
    public string? Interests { get; set; }
    public string? Education { get; set; }
    public string? WorkExperience { get; set; }
    public string? Certifications { get; set; }
    public string? SocialLinks { get; set; }
    public DateTime? LastUpdated { get; set; }

    // Navigation property
    public ApplicationUser User { get; set; } = null!;
}