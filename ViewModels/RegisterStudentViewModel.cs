using System.ComponentModel.DataAnnotations;

namespace EduvisionMvc.ViewModels;

public class RegisterStudentViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [Range(15, 100)]
    public int Age { get; set; } = 18;

    [Required]
    [StringLength(64)]
    public string Major { get; set; } = "Undeclared";

    // Optional contact and academic level
    [Phone]
    public string? Phone { get; set; }

    [Display(Name = "Academic Level")]
    public string? AcademicLevel { get; set; } // Freshman, Sophomore, etc.

    // Optional: allow setting an enrollment date
    [DataType(DataType.Date)]
    public DateTime? EnrollmentDate { get; set; }

    [Required]
    [Display(Name = "Department")]
    public int DepartmentId { get; set; }
}