using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using EduvisionMvc.Models;
using EduvisionMvc.Services;
using EduvisionMvc.Data;
using EduvisionMvc.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EduvisionMvc.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILoginRedirectService _redirectService;
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILoginRedirectService redirectService,
        AppDbContext db,
        IConfiguration config)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _redirectService = redirectService;
        _db = db;
        _config = config;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View();
        }

        var result = await _signInManager.PasswordSignInAsync(email, password, isPersistent: false, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                // Update last login timestamp
                user.LastLoginDate = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                var redirectResult = await _redirectService.GetRedirectResultAsync(user.Id);
                return redirectResult;
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View();
    }

    [HttpGet]
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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterStudentViewModel model, string role = "Student")
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
                        TotalCredits = 0,
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
                var hasEnrollments = _db.Enrollments.Any(e => e.Student!.UserId == user.Id && !e.Numeric_Grade.HasValue);
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

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
}
