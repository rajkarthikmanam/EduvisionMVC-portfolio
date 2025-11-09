using System.ComponentModel.DataAnnotations;

namespace EduvisionMvc.ViewModels;

public class InstructorRegisterViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [Display(Name="Department")]
    public int DepartmentId { get; set; }

    [Required]
    [Display(Name="Access Code")]
    public string AccessCode { get; set; } = string.Empty;
}