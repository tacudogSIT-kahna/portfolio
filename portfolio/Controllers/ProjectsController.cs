using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Npgsql;
using portfolio.Data;
using portfolio.Models;

namespace portfolio.Controllers;

// The EDITOR side for projects (and, in the same form, their images).
// Same layout as BooksController / MembersController in the library project.
[Authorize]
public class ProjectsController : Controller
{
    private readonly ProjectRepository _projects;
    private readonly ProjectImageRepository _images;
    private readonly CourseRepository _courses;

    public ProjectsController(ProjectRepository projects, ProjectImageRepository images, CourseRepository courses)
    {
        _projects = projects;
        _images = images;
        _courses = courses;
    }

    // Fills the Course dropdown. Call it again whenever the form is shown again.
    private async Task FillDropdownsAsync()
    {
        ViewBag.Courses = new SelectList(await _courses.GetAllAsync(), "CourseId", "Code");
    }

    // GET /Projects : the editor's table of projects.
    public async Task<IActionResult> Index()
    {
        var projects = await _projects.GetAllAsync();
        return View(projects);
    }

    // GET /Projects/Create : an empty form.
    public async Task<IActionResult> Create()
    {
        await FillDropdownsAsync();
        return View(new Project { StartedOn = DateOnly.FromDateTime(DateTime.Today) });
    }

    // POST /Projects/Create : the project and its images are saved in ONE transaction.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Project project)
    {
        if (!ModelState.IsValid)
        {
            await FillDropdownsAsync();
            return View(project);
        }

        try
        {
            var id = await _projects.AddWithImagesAsync(project, ImageLines.Split(project.ImageUrls));
            return RedirectToAction("ProjectDetails", "Portfolio", new { id });
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.CheckViolation
                                    && ex.ConstraintName == "chk_project_started")
        {
            ModelState.AddModelError("StartedOn", "The start date cannot be in the future. Nothing was saved.");
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.CheckViolation
                                           && ex.ConstraintName == "chk_project_ended")
        {
            ModelState.AddModelError("EndedOn", "The end date cannot be in the future. Nothing was saved.");
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.CheckViolation
                                           && ex.ConstraintName == "chk_project_dates")
        {
            ModelState.AddModelError("EndedOn", "The end date cannot be before the start date. Nothing was saved.");
        }

        await FillDropdownsAsync();
        return View(project);
    }

    // GET /Projects/Edit/5 : the form filled with project 5 and its images, one per line.
    public async Task<IActionResult> Edit(long id)
    {
        var project = await _projects.GetByIdAsync(id);
        if (project == null)
        {
            return NotFound();
        }

        var images = await _images.GetByProjectAsync(id);
        project.ImageUrls = string.Join(Environment.NewLine, images.Select(i => i.ImageUrl));

        await FillDropdownsAsync();
        return View(project);
    }

    // POST /Projects/Edit : the columns and the image list are saved in ONE transaction.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Project project)
    {
        if (!ModelState.IsValid)
        {
            await FillDropdownsAsync();
            return View(project);
        }

        try
        {
            if (!await _projects.UpdateWithImagesAsync(project, ImageLines.Split(project.ImageUrls)))
            {
                return NotFound();                               // the project was deleted meanwhile
            }
            return RedirectToAction("ProjectDetails", "Portfolio", new { id = project.ProjectId });
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.CheckViolation
                                   && ex.ConstraintName == "chk_project_started")
        {
            ModelState.AddModelError("StartedOn", "The start date cannot be in the future. Nothing was saved.");
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.CheckViolation
                                           && ex.ConstraintName == "chk_project_ended")
        {
            ModelState.AddModelError("EndedOn", "The end date cannot be in the future. Nothing was saved.");
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.CheckViolation
                                           && ex.ConstraintName == "chk_project_dates")
        {
            ModelState.AddModelError("EndedOn", "The end date cannot be before the start date. Nothing was saved.");
        }

        await FillDropdownsAsync();
        return View(project);
    }

    // GET /Projects/Delete/5 : ask "are you sure?"
    public async Task<IActionResult> Delete(long id)
    {
        var project = await _projects.GetByIdAsync(id);
        if (project == null)
        {
            return NotFound();
        }
        return View(project);
    }

    // POST /Projects/Delete/5 : the delete was confirmed. The project's images are deleted with it (CASCADE).
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        await _projects.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }
}