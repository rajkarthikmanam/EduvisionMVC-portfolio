using Microsoft.AspNetCore.Mvc;

public class PagesController : Controller
{
    // route: /api
    [HttpGet("/api")]
    public IActionResult Api() => View("~/Views/Api/Index.cshtml");
}
