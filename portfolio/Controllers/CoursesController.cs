using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using portfolio.Data;
using portfolio.Models;

namespace portfolio.Controllers;

// The EDITOR side for courses. [Authorize] refuses every visitor who is not signed in.
[Authorize]
public class CoursesController : Controller
{
    private readonly CourseRepository _courses;

    public CoursesController(CourseRepository courses)
    {
        _courses = courses;
    }

    // GET /Courses : the editor's table of courses.
    public async Task<IActionResult> Index()
    {
        return View(await _courses.GetAllAsync());
    }

    // GET /Courses/Create : an empty form.
    public IActionResult Create()
    {
        return View(new Course());
    }

    // POST /Courses/Create : the form was submitted.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Course course)
    {
        if (!ModelState.IsValid)
        {
            return View(course);
        }

        try
        {
            await _courses.AddAsync(course);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)          // 23505
        {
            ModelState.AddModelError("Code", "Another course already uses this code.");
            return View(course);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.CheckViolation)           // 23514
        {
            ModelState.AddModelError("Units", "The database refused these units. Use a number from 1 to 6.");
            return View(course);
        }

        return RedirectToAction(nameof(Index));
    }

    // GET /Courses/Edit/4
    public async Task<IActionResult> Edit(long id)
    {
        var course = await _courses.GetByIdAsync(id);
        if (course == null)
        {
            return NotFound();
        }
        return View(course);
    }

    // POST /Courses/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Course course)
    {
        if (!ModelState.IsValid)
        {
            return View(course);
        }

        try
        {
            if (!await _courses.UpdateAsync(course))
            {
                return NotFound();                               // the course was deleted meanwhile
            }
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            ModelState.AddModelError("Code", "Another course already uses this code.");
            return View(course);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.CheckViolation)
        {
            ModelState.AddModelError("Units", "The database refused these units. Use a number from 1 to 6.");
            return View(course);
        }

        return RedirectToAction(nameof(Index));
    }

    // GET /Courses/Delete/4 : "are you sure?"
    public async Task<IActionResult> Delete(long id)
    {
        var course = await _courses.GetByIdAsync(id);
        if (course == null)
        {
            return NotFound();
        }
        return View(course);
    }

    // POST /Courses/Delete/4 : confirmed. The database refuses when the course still has projects.
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        try
        {
            await _courses.DeleteAsync(id);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)      // 23503
        {
            var course = await _courses.GetByIdAsync(id);
            if (course == null)
            {
                return NotFound();
            }
            ViewBag.Error = "This course still has projects, so the database will not delete it. Delete or move its projects first.";
            return View("Delete", course);
        }

        return RedirectToAction(nameof(Index));
    }


}