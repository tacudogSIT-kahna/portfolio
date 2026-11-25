using Npgsql;
using portfolio.Models;

namespace portfolio.Data;

// Every SQL statement about projects lives in this one class.
public class ProjectRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ProjectImageRepository _images;     // used only to run image statements inside our transaction

    public ProjectRepository(NpgsqlDataSource dataSource, ProjectImageRepository images)
    {
        _dataSource = dataSource;
        _images = images;
    }

    // Every project SELECT starts this way: positions 0 to 9 match ReadProject.
    //  0 project_id  1 course_id  2 title  3 summary  4 video_url  5 completed_on
    //  6 course code  7 course title  8 image count  9 cover image (the first image, or NULL)
    private const string SelectProjects =
        "SELECT p.project_id, p.course_id, p.title, p.summary, p.video_url, p.completed_on, " +
        "       c.code, c.title, COUNT(i.image_id), " +
        "       (SELECT i2.image_url FROM portfolio.project_image i2 " +
        "        WHERE i2.project_id = p.project_id ORDER BY i2.image_id LIMIT 1) " +
        "FROM portfolio.project p " +
        "JOIN portfolio.course c ON c.course_id = p.course_id " +
        "LEFT JOIN portfolio.project_image i ON i.project_id = p.project_id ";

    private const string GroupBy = "GROUP BY p.project_id, c.code, c.title ";

    private static Project ReadProject(NpgsqlDataReader reader)
    {
        return new Project
        {
            ProjectId = reader.GetInt64(0),
            CourseId = reader.GetInt64(1),
            Title = reader.GetString(2),
            Summary = reader.IsDBNull(3) ? null : reader.GetString(3),
            VideoUrl = reader.IsDBNull(4) ? null : reader.GetString(4),
            CompletedOn = reader.GetFieldValue<DateOnly>(5),
            CourseCode = reader.GetString(6),
            CourseTitle = reader.GetString(7),
            ImageCount = reader.GetInt64(8),
            CoverUrl = reader.IsDBNull(9) ? null : reader.GetString(9)
        };
    }

    private async Task<List<Project>> ReadListAsync(NpgsqlCommand command)
    {
        var projects = new List<Project>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            projects.Add(ReadProject(reader));
        }
        return projects;
    }

    // READ: every project, newest first.
    public async Task<List<Project>> GetAllAsync()
    {
        const string sql = SelectProjects + GroupBy + "ORDER BY p.completed_on DESC, p.project_id DESC;";
        await using var command = _dataSource.CreateCommand(sql);
        return await ReadListAsync(command);
    }

    // READ: the projects of one course.
    public async Task<List<Project>> GetByCourseAsync(long courseId)
    {
        const string sql = SelectProjects + "WHERE p.course_id = @course_id " + GroupBy +
                           "ORDER BY p.completed_on DESC, p.project_id DESC;";
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("course_id", courseId);
        return await ReadListAsync(command);
    }

    // READ: search the title and summary (the viewer's search box). ILIKE ignores capitals.
    public async Task<List<Project>> SearchAsync(string text)
    {
        const string sql = SelectProjects +
                           "WHERE p.title ILIKE '%' || @q || '%' " +
                           "   OR p.summary ILIKE '%' || @q || '%' " + GroupBy +
                           "ORDER BY p.title;";
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("q", text);
        return await ReadListAsync(command);
    }

    // READ: one project, or null.
    public async Task<Project?> GetByIdAsync(long id)
    {
        const string sql = SelectProjects + "WHERE p.project_id = @id " + GroupBy + ";";
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadProject(reader) : null;
    }

    // Puts the project's five columns into a command's parameters.
    private static void AddProjectParameters(NpgsqlCommand command, Project project)
    {
        command.Parameters.AddWithValue("course_id", project.CourseId!.Value);
        command.Parameters.AddWithValue("title", project.Title);
        command.Parameters.AddWithValue("summary", (object?)project.Summary ?? DBNull.Value);
        command.Parameters.AddWithValue("video_url", (object?)project.VideoUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("completed_on", project.CompletedOn!.Value);
    }

    // CREATE: a project AND its images, inside ONE transaction.
    // If any statement fails (for example the same image URL twice), the whole transaction is
    // rolled back and nothing is saved: no project, no images. The exception is then re-thrown
    // so the controller can show a message.
    public async Task<long> AddWithImagesAsync(Project project, IEnumerable<string> imageUrls)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            long newId;
            await using (var insert = new NpgsqlCommand(
                "INSERT INTO portfolio.project (course_id, title, summary, video_url, completed_on) " +
                "VALUES (@course_id, @title, @summary, @video_url, @completed_on) " +
                "RETURNING project_id;", connection, transaction))
            {
                AddProjectParameters(insert, project);
                newId = (long)(await insert.ExecuteScalarAsync())!;      // the id the database just made
            }

            foreach (var url in imageUrls)
            {
                await _images.InsertAsync(connection, transaction, newId, url);
            }

            await transaction.CommitAsync();       // everything worked: keep it all
            return newId;
        }
        catch
        {
            await transaction.RollbackAsync();     // something failed: undo it all
            throw;
        }
    }

    // UPDATE: the project's columns AND its image list, inside ONE transaction.
    // The old images are deleted and the new list inserted; if any step fails, the old images stay.
    // Returns false (and changes nothing) when no project has that id.
    public async Task<bool> UpdateWithImagesAsync(Project project, IEnumerable<string> imageUrls)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            int changed;
            await using (var update = new NpgsqlCommand(
                "UPDATE portfolio.project " +
                "SET course_id = @course_id, title = @title, summary = @summary, " +
                "    video_url = @video_url, completed_on = @completed_on " +
                "WHERE project_id = @id;", connection, transaction))
            {
                AddProjectParameters(update, project);
                update.Parameters.AddWithValue("id", project.ProjectId);
                changed = await update.ExecuteNonQueryAsync();
            }

            if (changed == 0)
            {
                await transaction.RollbackAsync();
                return false;
            }

            await _images.DeleteByProjectAsync(connection, transaction, project.ProjectId);
            foreach (var url in imageUrls)
            {
                await _images.InsertAsync(connection, transaction, project.ProjectId, url);
            }

            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // DELETE: the project; its images go with it (ON DELETE CASCADE).
    public async Task DeleteAsync(long id)
    {
        const string sql = "DELETE FROM portfolio.project WHERE project_id = @id;";
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync();
    }
}
