using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace EduvisionMvc.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApiController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    public ApiController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("books")]
    public async Task<IActionResult> GetBooks([FromQuery] string q = "education")
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"https://openlibrary.org/search.json?q={Uri.EscapeDataString(q)}";

        try
        {
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            var items = doc.RootElement.GetProperty("docs")
                .EnumerateArray()
                .Take(10)
                .Select(x => new
                {
                    title = x.TryGetProperty("title", out var t) ? t.GetString() : "",
                    author = x.TryGetProperty("author_name", out var a) ? string.Join(", ", a.EnumerateArray().Select(ae => ae.GetString())) : "",
                    year = x.TryGetProperty("first_publish_year", out var y) ? y.GetInt32().ToString() : "N/A"
                });

            return Ok(new { ok = true, source = "OpenLibrary", items });
        }
        catch (Exception ex)
        {
            return BadRequest(new { ok = false, error = ex.Message });
        }
    }
}
