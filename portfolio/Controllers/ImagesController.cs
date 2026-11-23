using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using portfolio.Data;
using portfolio.Models;

namespace portfolio.Controllers;

// The EDITOR side for single images: list, add, edit and delete the images of ONE project.
[Authorize]
public class ImagesController : Controller
{
    private readonly ProjectImageRepository _images;
    private readonly ProjectRepository _projects;

    public ImagesController(ProjectImageRepository images, ProjectRepository projects)
    {
        _images = images;
        _projects = projects;
    }

    // GET /Images?projectId=5 : the images of project 5.
    public async Task<IActionResult> Index(long projectId)
    {
        var project = await _projects.GetByIdAsync(projectId);
        if (project == null)
        {
            return NotFound();
        }

        ViewBag.Project = project;
        return View(await _images.GetByProjectAsync(projectId));
    }

    // GET /Images/Create?projectId=5
    public async Task<IActionResult> Create(long projectId)
    {
        var project = await _projects.GetByIdAsync(projectId);
        if (project == null)
        {
            return NotFound();
        }
        return View(new ProjectImage { ProjectId = projectId, ProjectTitle = project.Title });
    }

    // POST /Images/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectImage image)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _images.AddAsync(image);
                return RedirectToAction(nameof(Index), new { projectId = image.ProjectId });
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)      // 23505
            {
                ModelState.AddModelError("ImageUrl", "This project already has that image URL.");
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)  // 23503
            {
                ModelState.AddModelError("", "That project no longer exists.");
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.CheckViolation)       // 23514
            {
                ModelState.AddModelError("ImageUrl", "The database refused this URL: it must start with http:// or https://.");
            }
        }

        image.ProjectTitle = (await _projects.GetByIdAsync(image.ProjectId))?.Title;
        return View(image);
    }

    // GET /Images/Edit/9
    public async Task<IActionResult> Edit(long id)
    {
        var image = await _images.GetByIdAsync(id);
        if (image == null)
        {
            return NotFound();
        }
        return View(image);
    }

    // POST /Images/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProjectImage image)
    {
        if (ModelState.IsValid)
        {
            try
            {
                if (!await _images.UpdateAsync(image))
                {
                    return NotFound();
                }
                return RedirectToAction(nameof(Index), new { projectId = image.ProjectId });
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                ModelState.AddModelError("ImageUrl", "This project already has that image URL.");
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.CheckViolation)
            {
                ModelState.AddModelError("ImageUrl", "The database refused this URL: it must start with http:// or https://.");
            }
        }

        image.ProjectTitle = (await _projects.GetByIdAsync(image.ProjectId))?.Title;
        return View(image);
    }

    // GET /Images/Delete/9
    public async Task<IActionResult> Delete(long id)
    {
        var image = await _images.GetByIdAsync(id);
        if (image == null)
        {
            return NotFound();
        }
        return View(image);
    }

    // POST /Images/Delete/9
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        var image = await _images.GetByIdAsync(id);
        if (image == null)
        {
            return NotFound();
        }

        await _images.DeleteAsync(id);
        return RedirectToAction(nameof(Index), new { projectId = image.ProjectId });
    }
}
