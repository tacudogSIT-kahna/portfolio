using System.ComponentModel.DataAnnotations;

namespace portfolio.Models;

// One Project object holds one row of portfolio.project, plus a few helper values for the pages.
public class Project
{
    public long ProjectId { get; set; }                     // project_id    BIGINT, the primary key

    [Required(ErrorMessage = "Choose a course.")]
    public long? CourseId { get; set; }                     // course_id     BIGINT NOT NULL, foreign key
                                                            // (long? so an unchosen dropdown arrives as null)
    [Required(ErrorMessage = "Enter the project title.")]
    [StringLength(100, ErrorMessage = "The title can be at most 100 characters.")]
    public string Title { get; set; } = "";                 // title         VARCHAR(100) NOT NULL

    [StringLength(2000, ErrorMessage = "The summary can be at most 2000 characters.")]
    public string? Summary { get; set; }                    // summary       TEXT, may be NULL

    [StringLength(200, ErrorMessage = "The video link can be at most 200 characters.")]
    [RegularExpression(Patterns.VideoUrl, ErrorMessage = "Paste a YouTube link, such as https://youtu.be/abcdefghijk")]
    public string? VideoUrl { get; set; }                   // video_url     VARCHAR(200), may be NULL

    [Required(ErrorMessage = "Choose the date you started it.")]
    public DateOnly? StartedOn { get; set; }                // started_on    DATE NOT NULL (the database refuses a future date)

    public DateOnly? EndedOn { get; set; }                  // ended_on      DATE, may be NULL = still ongoing

    [StringLength(4000, ErrorMessage = "The notes can be at most 4000 characters.")]
    public string? Notes { get; set; }                      // notes         TEXT, may be NULL (the notepad)

    // ---- Not columns of this table: filled by the join, or used only by the form ----
    public string? CourseCode { get; set; }                 // from portfolio.course
    public string? CourseTitle { get; set; }                // from portfolio.course
    public long ImageCount { get; set; }                    // LEFT JOIN + COUNT(image_id)
    public string? CoverUrl { get; set; }                   // the project's first image, or null

    [ImageUrlLines]
    public string? ImageUrls { get; set; }                  // the form's box: one image URL per line
}