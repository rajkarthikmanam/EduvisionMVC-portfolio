using Microsoft.AspNetCore.Mvc;

namespace EduvisionMvc.Controllers;

public class ChartsController : Controller
{
    // GET /charts
    [HttpGet("/charts")]
    public IActionResult Index() => View();
}
