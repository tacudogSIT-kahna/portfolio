using Microsoft.AspNetCore.Mvc;
using portfolio.Data;
using portfolio.Models;

namespace portfolio.Controllers;

// The VIEWER side: read-only, open to every visitor. No sign-in, no forms that change data.
public class PortfolioController : Controller
{
    private readonly CourseRepository _courses;
    private readonly ProjectRepository _projects;
    private readonly ProjectImageRepository _images;

    public PortfolioController(CourseRepository courses, ProjectRepository projects, ProjectImageRepository images)
    {
        _courses = courses;
        _projects = projects;
        _images = images;
    }

    // GET / or /Portfolio : the courses, plus search results when ?q= is given.
    public async Task<IActionResult> Index(string? q)
    {
        ViewBag.Query = q;
        if (!string.IsNullOrWhiteSpace(q))
        {
            ViewBag.Results = await _projects.SearchAsync(q.Trim());
        }

        var courses = await _courses.GetAllAsync();
        return View(courses);
    }

    // GET /Portfolio/CourseDetails/4 : one course and its projects.
    public async Task<IActionResult> CourseDetails(long id)
    {
        var course = await _courses.GetByIdAsync(id);
        if (course == null)
        {
            return NotFound();                                   // 404 for an id that does not exist
        }

        ViewBag.Projects = await _projects.GetByCourseAsync(id);
        return View(course);
    }

    // GET /Portfolio/ProjectDetails/5 : one project, its images and its video.
    public async Task<IActionResult> ProjectDetails(long id)
    {
        var project = await _projects.GetByIdAsync(id);
        if (project == null)
        {
            return NotFound();
        }

        ViewBag.Images = await _images.GetByProjectAsync(id);
        return View(project);
    }
}
