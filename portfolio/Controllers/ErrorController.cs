using Microsoft.AspNetCore.Mvc;

namespace portfolio.Controllers;

// The friendly pages. Program.cs sends visitors here instead of showing a raw error.
public class ErrorController : Controller
{
    // /Error : anything unexpected (an unhandled exception, status 500).
    public IActionResult Index()
    {
        return View();
    }

    // /Error/Status/404 : a status code with no page of its own (404 not found, 403, 400 ...).
    public IActionResult Status(int id)
    {
        ViewBag.Code = id;
        return View();
    }
}