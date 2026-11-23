using Npgsql;
using portfolio.Models;

namespace portfolio.Data;

// Every SQL statement about courses lives in this one class.
public class CourseRepository
{
    private readonly NpgsqlDataSource _dataSource;

    // ASP.NET Core hands in the data source that Program.cs registered.
    public CourseRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    // Every course SELECT starts the same way, so ReadCourse can read by position 0 to 6.
    // LEFT JOIN + COUNT(p.project_id): a course with no projects is kept, with a count of 0.
    private const string SelectCourses =
        "SELECT c.course_id, c.code, c.title, c.units, c.term_taken, c.description, COUNT(p.project_id) " +
        "FROM portfolio.course c " +
        "LEFT JOIN portfolio.project p ON p.course_id = c.course_id ";

    // Copies the current row of the reader into a Course.
    private static Course ReadCourse(NpgsqlDataReader reader)
    {
        return new Course
        {
            CourseId = reader.GetInt64(0),
            Code = reader.GetString(1),
            Title = reader.GetString(2),
            Units = reader.GetInt32(3),
            TermTaken = reader.GetString(4),
            Description = reader.IsDBNull(5) ? null : reader.GetString(5),   // NULL becomes null
            ProjectCount = reader.GetInt64(6)
        };
    }

    // READ: every course, sorted by code.
    public async Task<List<Course>> GetAllAsync()
    {
        const string sql = SelectCourses + "GROUP BY c.course_id ORDER BY c.code;";

        var courses = new List<Course>();
        await using var command = _dataSource.CreateCommand(sql);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            courses.Add(ReadCourse(reader));
        }
        return courses;
    }

    // CREATE: the database chooses course_id.
    public async Task AddAsync(Course course)
    {
        const string sql = "INSERT INTO portfolio.course (code, title, units, term_taken, description) " +
                           "VALUES (@code, @title, @units, @term_taken, @description);";

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("code", course.Code);
        command.Parameters.AddWithValue("title", course.Title);
        command.Parameters.AddWithValue("units", course.Units);
        command.Parameters.AddWithValue("term_taken", course.TermTaken);
        command.Parameters.AddWithValue("description", (object?)course.Description ?? DBNull.Value);
        await command.ExecuteNonQueryAsync();
    }
}