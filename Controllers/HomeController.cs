using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EduvisionMvc.Models;

namespace EduvisionMvc.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    // Home - Allow anonymous access
    [AllowAnonymous]
    public IActionResult Index() => View();

    // NEW: Visualization page
    [AllowAnonymous]
    public IActionResult Visualize() => View();

    // NEW: About page
    [AllowAnonymous]
    public IActionResult About() => View();

    // (Optional) keep Privacy or remove if unused
    [AllowAnonymous]
    public IActionResult Privacy() => View();

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
