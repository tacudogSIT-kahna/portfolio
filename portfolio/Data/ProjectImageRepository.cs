using Npgsql;
using portfolio.Models;

namespace portfolio.Data;

// Every SQL statement about project images lives in this one class.
public class ProjectImageRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public ProjectImageRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    // Positions 0 to 3: image_id, project_id, image_url, and the project's title (JOIN).
    private const string SelectImages =
        "SELECT i.image_id, i.project_id, i.image_url, p.title " +
        "FROM portfolio.project_image i " +
        "JOIN portfolio.project p ON p.project_id = i.project_id ";

    private static ProjectImage ReadImage(NpgsqlDataReader reader)
    {
        return new ProjectImage
        {
            ImageId = reader.GetInt64(0),
            ProjectId = reader.GetInt64(1),
            ImageUrl = reader.GetString(2),
            ProjectTitle = reader.GetString(3)
        };
    }

    // READ: the images of one project, in the order they were added.
    public async Task<List<ProjectImage>> GetByProjectAsync(long projectId)
    {
        const string sql = SelectImages + "WHERE i.project_id = @project_id ORDER BY i.image_id;";

        var images = new List<ProjectImage>();
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("project_id", projectId);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            images.Add(ReadImage(reader));
        }
        return images;
    }

    // READ: one image, or null.
    public async Task<ProjectImage?> GetByIdAsync(long id)
    {
        const string sql = SelectImages + "WHERE i.image_id = @id;";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadImage(reader) : null;
    }

    // CREATE: one image on its own (the editor's "Add an image" page).
    public async Task AddAsync(ProjectImage image)
    {
        const string sql = "INSERT INTO portfolio.project_image (project_id, image_url) VALUES (@project_id, @image_url);";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("project_id", image.ProjectId);
        command.Parameters.AddWithValue("image_url", image.ImageUrl.Trim());
        await command.ExecuteNonQueryAsync();
    }

    // UPDATE: change the URL of one image. Returns false when no image has that id.
    public async Task<bool> UpdateAsync(ProjectImage image)
    {
        const string sql = "UPDATE portfolio.project_image SET image_url = @image_url WHERE image_id = @id;";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("image_url", image.ImageUrl.Trim());
        command.Parameters.AddWithValue("id", image.ImageId);
        return await command.ExecuteNonQueryAsync() > 0;
    }

    // DELETE: one image.
    public async Task DeleteAsync(long id)
    {
        const string sql = "DELETE FROM portfolio.project_image WHERE image_id = @id;";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync();
    }

    // ---- The two methods below do NOT open their own connection. ----
    // They run on the connection and transaction that ProjectRepository opened,
    // so that a project and all of its images are saved together or not at all.

    // INSERT one image inside someone else's transaction.
    public async Task InsertAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, long projectId, string url)
    {
        await using var command = new NpgsqlCommand(
            "INSERT INTO portfolio.project_image (project_id, image_url) VALUES (@project_id, @image_url);",
            connection, transaction);
        command.Parameters.AddWithValue("project_id", projectId);
        command.Parameters.AddWithValue("image_url", url);
        await command.ExecuteNonQueryAsync();
    }

    // DELETE every image of a project inside someone else's transaction.
    public async Task DeleteByProjectAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, long projectId)
    {
        await using var command = new NpgsqlCommand(
            "DELETE FROM portfolio.project_image WHERE project_id = @project_id;",
            connection, transaction);
        command.Parameters.AddWithValue("project_id", projectId);
        await command.ExecuteNonQueryAsync();
    }
}
