using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using EduvisionMvc.Models;

namespace EduvisionMvc.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    // Home
    public IActionResult Index() => View();

    // NEW: Visualization page
    public IActionResult Visualize() => View();

    // NEW: About page
    public IActionResult About() => View();

    // (Optional) keep Privacy or remove if unused
    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
