using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduvisionMvc.Data;
using EduvisionMvc.Models;
using System.Text;

namespace EduvisionMvc.Controllers;

[Authorize(Roles = "Admin")]
public class DiagnosticsController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DiagnosticsController(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> CheckStudentLinks()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== IDENTITY USERS WITH STUDENT ROLE ===\n");

        var studentUsers = await _userManager.GetUsersInRoleAsync("Student");
        
        foreach (var user in studentUsers)
        {
            sb.AppendLine($"User ID: {user.Id}");
            sb.AppendLine($"UserName: {user.UserName}");
            sb.AppendLine($"Email: {user.Email}");
            sb.AppendLine($"StudentId FK: {user.StudentId}");
            
            // Check if Student record exists with matching UserId
            var studentByUserId = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (studentByUserId != null)
            {
                sb.AppendLine($"✓ Student found by UserId: ID={studentByUserId.Id}, Name={studentByUserId.Name}, Email={studentByUserId.Email}");
            }
            else
            {
                sb.AppendLine($"✗ NO Student record with UserId={user.Id}");
            }
            
            // Check if StudentId FK points to valid Student
            if (user.StudentId.HasValue)
            {
                var studentByFK = await _context.Students.FindAsync(user.StudentId.Value);
                if (studentByFK != null)
                {
                    sb.AppendLine($"  StudentId FK points to: ID={studentByFK.Id}, Name={studentByFK.Name}, UserId={studentByFK.UserId}");
                    if (studentByFK.UserId != user.Id)
                    {
                        sb.AppendLine($"  ⚠ WARNING: Student.UserId ({studentByFK.UserId}) != ApplicationUser.Id ({user.Id})");
                    }
                }
                else
                {
                    sb.AppendLine($"  ✗ StudentId FK ({user.StudentId}) points to NON-EXISTENT Student record!");
                }
            }
            else
            {
                sb.AppendLine($"  StudentId FK is NULL");
            }
            
            sb.AppendLine("---\n");
        }

        return Content(sb.ToString(), "text/plain");
    }
}
