using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using EduvisionMvc.Models;
using EduvisionMvc.Services;
using EduvisionMvc.Data;
using EduvisionMvc.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EduvisionMvc.Controllers;
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILoginRedirectService _redirectService;
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILoginRedirectService redirectService,
        AppDbContext db,
        IConfiguration config,
        IWebHostEnvironment env)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _redirectService = redirectService;
        _db = db;
        _config = config;
        _env = env;
    }

    private async Task EnsureDomainLinksByEmailAsync(ApplicationUser user, string emailInput)
    {
        var email = (user.Email ?? emailInput ?? user.UserName ?? string.Empty).Trim();
        // STUDENT
        if (await _userManager.IsInRoleAsync(user, "Student"))
        {
            // Try finding by UserId first (most reliable), then by email
            var student = await _db.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (student == null && !string.IsNullOrEmpty(email))
            {
                student = await _db.Students.FirstOrDefaultAsync(s => s.Email.ToLower() == email.ToLower());
            }
            if (student == null)
            {
                var dept = await _db.Departments.FirstOrDefaultAsync() ?? new Department { Name = "General" };
                if (dept.Id == 0) { _db.Departments.Add(dept); await _db.SaveChangesAsync(); }
                student = new Student
                {
                    UserId = user.Id,
                    Name = ($"{user.FirstName} {user.LastName}").Trim(),
                    Email = email,
                    Major = dept.Name,
                    DepartmentId = dept.Id,
                    EnrollmentDate = DateTime.UtcNow.Date,
                    Gpa = 0m,
                    TotalCredits = 120 // Credits required for graduation
                };
                _db.Students.Add(student);
                await _db.SaveChangesAsync();
            }
            // Always link user to the student entity found by email
            if (user.StudentId != student.Id)
            {
                user.StudentId = student.Id;
                await _userManager.UpdateAsync(user);
            }
            // Ensure back-link (optional) remains correct
            if (student.UserId != user.Id)
            {
                student.UserId = user.Id;
                _db.Update(student);
                await _db.SaveChangesAsync();
            }
        }

        // INSTRUCTOR
        if (await _userManager.IsInRoleAsync(user, "Instructor"))
        {
            // Try finding by UserId first (most reliable), then by email
            var instructor = await _db.Instructors.FirstOrDefaultAsync(i => i.UserId == user.Id);
            if (instructor == null && !string.IsNullOrEmpty(email))
            {
                instructor = await _db.Instructors.FirstOrDefaultAsync(i => i.Email.ToLower() == email.ToLower());
            }
            if (instructor == null)
            {
                var dept = await _db.Departments.FirstOrDefaultAsync() ?? new Department { Name = "General" };
                if (dept.Id == 0) { _db.Departments.Add(dept); await _db.SaveChangesAsync(); }
                instructor = new Instructor
                {
                    UserId = user.Id,
                    FirstName = user.FirstName ?? "Instructor",
                    LastName = user.LastName ?? "User",
                    Email = email,
                    DepartmentId = dept.Id
                };
                _db.Instructors.Add(instructor);
                await _db.SaveChangesAsync();
            }
            if (user.InstructorId != instructor.Id)
            {
                user.InstructorId = instructor.Id;
                await _userManager.UpdateAsync(user);
            }
            if (instructor.UserId != user.Id)
            {
                instructor.UserId = user.Id;
                _db.Update(instructor);
                await _db.SaveChangesAsync();
            }
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        // If already authenticated, skip the login page and redirect to the correct dashboard
        if (User?.Identity?.IsAuthenticated == true)
        {
            var user = _userManager.GetUserAsync(User).GetAwaiter().GetResult();
            if (user != null)
            {
                var redirect = _redirectService.GetRedirectResultAsync(user.Id).GetAwaiter().GetResult();
                return redirect;
            }
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        
        try
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View();
            }

            // Try finding user by email first, then fall back to username (supports demo1, etc.)
            var user = await _userManager.FindByEmailAsync(email.Trim());
            if (user == null)
            {
                user = await _userManager.FindByNameAsync(email.Trim());
            }
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: false);
            if (result.Succeeded)
        {
            if (user != null)
            {
                // Update last login timestamp
                user.LastLoginDate = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                // --- Ensure domain profile linkage (auto-heal) ---
                await EnsureDomainLinksByEmailAsync(user, email);

                var redirectResult = await _redirectService.GetRedirectResultAsync(user.Id);
                return redirectResult;
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        // DEV convenience: auto-reset known demo account passwords if they drifted
        if (_env.IsDevelopment())
        {
            var demoPasswords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["admin@local.test"] = "Admin123!",
                ["instructor@local.test"] = "Instructor123!",
                ["math.prof@local.test"] = "Math123!",
                ["eng.prof@local.test"] = "Eng123!",
                ["bus.prof@local.test"] = "Bus123!",
                ["student@local.test"] = "Student123!"
            };

            if (demoPasswords.TryGetValue(email.Trim(), out var demoPw))
            {
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetRes = await _userManager.ResetPasswordAsync(user, resetToken, demoPw);
                if (resetRes.Succeeded)
                {
                    // try sign-in again with demo password
                    var retry = await _signInManager.PasswordSignInAsync(user, demoPw, isPersistent: false, lockoutOnFailure: false);
                    if (retry.Succeeded)
                    {
                        // mimic success path
                        user.LastLoginDate = DateTime.UtcNow;
                        await _userManager.UpdateAsync(user);

                        await EnsureDomainLinksByEmailAsync(user, email);

                        var redirectAfterReset = await _redirectService.GetRedirectResultAsync(user.Id);
                        return redirectAfterReset;
                    }
                }
            }
        }

        // Bubble up more specific states when possible
        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Account locked out. Try again later.");
        }
        else if (result.RequiresTwoFactor)
        {
            ModelState.AddModelError(string.Empty, "Two-factor authentication is required.");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        }
        return View();
        }
        catch (Exception ex)
        {
            // Log the full exception for debugging
            ModelState.AddModelError(string.Empty, $"Login error: {ex.Message}");
            return View();
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        // Render student-focused registration form
        var vm = new RegisterStudentViewModel
        {
            EnrollmentDate = DateTime.UtcNow.Date
        };
        ViewBag.Departments = new SelectList(_db.Departments.OrderBy(d => d.Name).ToList(), "Id", "Name");
        return View(vm);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterStudentViewModel model, string role = "Student")
    {
        try
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Departments = new SelectList(_db.Departments.OrderBy(d => d.Name).ToList(), "Id", "Name", model.DepartmentId);
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true,
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
        {
            // Ensure requested role exists; if not, fall back to Student
            if (!await _userManager.IsInRoleAsync(user, role))
            {
                var roles = new[] { "Admin", "Instructor", "Student" };
                if (!roles.Contains(role)) role = "Student";
                try
                {
                    await _userManager.AddToRoleAsync(user, role);
                }
                catch
                {
                    // If role assignment fails, default to Student
                    if (role != "Student")
                    {
                        await _userManager.AddToRoleAsync(user, "Student");
                        role = "Student";
                    }
                }
            }

            // Create domain entity for dashboards
            if (role == "Student")
            {
                var existingStudent = _db.Students.FirstOrDefault(s => s.UserId == user.Id);
                if (existingStudent == null)
                {
                    var dept = _db.Departments.FirstOrDefault(d => d.Id == model.DepartmentId) ?? _db.Departments.First();
                    // Pick a random advisor (prefer same department)
                    var deptAdvisors = _db.Instructors.Where(i => i.DepartmentId == dept.Id).ToList();
                    if (deptAdvisors.Count == 0)
                    {
                        deptAdvisors = _db.Instructors.ToList();
                    }
                    int? advisorId = null;
                    if (deptAdvisors.Count > 0)
                    {
                        var rand = Random.Shared.Next(deptAdvisors.Count);
                        advisorId = deptAdvisors[rand].Id;
                    }

                    var student = new Student
                    {
                        UserId = user.Id,
                        Name = $"{user.FirstName} {user.LastName}".Trim(),
                        Email = user.Email ?? user.UserName ?? model.Email,
                        Major = string.IsNullOrWhiteSpace(model.Major) ? dept.Name : model.Major.Trim(),
                        DepartmentId = dept.Id,
                        EnrollmentDate = model.EnrollmentDate ?? DateTime.UtcNow.Date,
                        Gpa = 0m,
                        TotalCredits = 120, // Credits required for graduation
                        Age = model.Age,
                        Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim(),
                        AcademicLevel = string.IsNullOrWhiteSpace(model.AcademicLevel) ? null : model.AcademicLevel.Trim(),
                        AdvisorInstructorId = advisorId
                    };
                    _db.Students.Add(student);
                    await _db.SaveChangesAsync();
                    user.StudentId = student.Id;
                    await _userManager.UpdateAsync(user);
                }
            }
            else if (role == "Instructor")
            {
                var existingInstructor = _db.Instructors.FirstOrDefault(i => i.UserId == user.Id);
                if (existingInstructor == null)
                {
                    var dept = _db.Departments.FirstOrDefault() ?? new Department { Name = "General" };
                    if (dept.Id == 0)
                    {
                        _db.Departments.Add(dept);
                        await _db.SaveChangesAsync();
                    }
                    var instructor = new Instructor
                    {
                        UserId = user.Id,
                        FirstName = user.FirstName ?? "Instructor",
                        LastName = user.LastName ?? "User",
                        Email = user.Email ?? user.UserName ?? model.Email,
                        DepartmentId = dept.Id
                    };
                    _db.Instructors.Add(instructor);
                    await _db.SaveChangesAsync();
                    user.InstructorId = instructor.Id;
                    await _userManager.UpdateAsync(user);
                }
            }

            await _signInManager.SignInAsync(user, isPersistent: false);

            // After registration, if student has no enrollments, send to enrollment page
            if (role == "Student")
            {
                var hasEnrollments = _db.Enrollments.Any(e => e.Student!.UserId == user.Id && !e.NumericGrade.HasValue);
                if (!hasEnrollments)
                {
                    return RedirectToAction("Index", "StudentCourses");
                }
            }

            var redirectResult = await _redirectService.GetRedirectResultAsync(user.Id);
            return redirectResult;
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(model);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Registration error: {ex.Message}");
            ViewBag.Departments = new SelectList(_db.Departments.OrderBy(d => d.Name).ToList(), "Id", "Name", model.DepartmentId);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // --- Instructor Registration ---
    [HttpGet]
    [AllowAnonymous]
    public IActionResult RegisterInstructor()
    {
        ViewBag.Departments = new SelectList(_db.Departments.OrderBy(d => d.Name).ToList(), "Id", "Name");
        return View(new InstructorRegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterInstructor(InstructorRegisterViewModel model)
    {
        try
        {
            var invite = _config["Registration:InstructorInviteCode"];
            if (string.IsNullOrWhiteSpace(invite))
            {
                ModelState.AddModelError(string.Empty, "Instructor registration is disabled.");
            }
            else if (!string.Equals(model.AccessCode?.Trim(), invite?.Trim(), StringComparison.Ordinal))
            {
                ModelState.AddModelError(nameof(model.AccessCode), "Invalid access code.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Departments = new SelectList(_db.Departments.OrderBy(d => d.Name).ToList(), "Id", "Name", model.DepartmentId);
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true,
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                ViewBag.Departments = new SelectList(_db.Departments.OrderBy(d => d.Name).ToList(), "Id", "Name", model.DepartmentId);
                return View(model);
            }

            // Role & Instructor entity
            await _userManager.AddToRoleAsync(user, "Instructor");

            var dept = _db.Departments.FirstOrDefault(d => d.Id == model.DepartmentId) ?? _db.Departments.First();
            var instructor = new Instructor
            {
                UserId = user.Id,
                FirstName = user.FirstName ?? "Instructor",
                LastName = user.LastName ?? "User",
                Email = user.Email ?? user.UserName ?? model.Email,
                DepartmentId = dept.Id
            };
            _db.Instructors.Add(instructor);
            await _db.SaveChangesAsync();
            user.InstructorId = instructor.Id;
            await _userManager.UpdateAsync(user);

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "InstructorDashboard");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Instructor registration error: {ex.Message}");
            ViewBag.Departments = new SelectList(_db.Departments.OrderBy(d => d.Name).ToList(), "Id", "Name", model.DepartmentId);
            return View(model);
        }
    }

    // --- Diagnostics endpoint: quickly view role/profile linkage for current user ---
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> LinkDiagnostics()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();
        var roles = await _userManager.GetRolesAsync(user);
        var hasStudent = user.StudentId != null && _db.Students.Any(s => s.Id == user.StudentId);
        var hasInstructor = user.InstructorId != null && _db.Instructors.Any(i => i.Id == user.InstructorId);
        return Json(new
        {
            user = user.Email,
            roles,
            studentId = user.StudentId,
            instructorId = user.InstructorId,
            hasStudent,
            hasInstructor
        });
    }
}
