using Microsoft.AspNetCore.Mvc;

namespace EduvisionMvc.Services;

public interface ILoginRedirectService
{
    Task<IActionResult> GetRedirectResultAsync(string userId);
}
