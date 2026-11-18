using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace portfolio.Controllers;

// Sign in and sign out for the editor side (extra credit).
// The editor password is read from configuration ("Editor:Password"), which on this computer is User Secrets.
// It is never written in any file of the repository.
public class AccountController : Controller
{
    private readonly IConfiguration _config;

    public AccountController(IConfiguration config)
    {
        _config = config;
    }

    // GET /Account/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    // POST /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string? password, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        var expected = _config["Editor:Password"];

        if (string.IsNullOrEmpty(expected))
        {
            ModelState.AddModelError("", "No editor password is set on this computer. Add Editor:Password in Manage User Secrets.");
            return View();
        }

        // Compare in fixed time, so the time taken does not hint at how many characters were right.
        bool correct = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(password ?? ""), Encoding.UTF8.GetBytes(expected));

        if (!correct)
        {
            ModelState.AddModelError("", "Wrong password.");
            return View();
        }

        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "Editor") },
                                          CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }
        return RedirectToAction("Index", "Courses");
    }

    // POST /Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Portfolio");
    }
}