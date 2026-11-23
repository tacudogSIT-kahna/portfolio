using System.ComponentModel.DataAnnotations;

namespace portfolio.Models;

// One Course object holds one row of portfolio.course.
public class Course
{
    public long CourseId { get; set; }                      // course_id   BIGINT, the primary key

    [Required(ErrorMessage = "Enter the course code.")]
    [StringLength(15, ErrorMessage = "The code can be at most 15 characters.")]
    [RegularExpression(Patterns.CourseCode, ErrorMessage = "Use capital letters then digits, such as IT 205 or CC101.")]
    public string Code { get; set; } = "";                  // code        VARCHAR(15) NOT NULL, UNIQUE

    [Required(ErrorMessage = "Enter the course title.")]
    [StringLength(100, ErrorMessage = "The title can be at most 100 characters.")]
    public string Title { get; set; } = "";                 // title       VARCHAR(100) NOT NULL

    [Range(1, 6, ErrorMessage = "Units must be from 1 to 6.")]
    public int Units { get; set; } = 3;                     // units       INTEGER NOT NULL, 1 to 6

    [Required(ErrorMessage = "Enter when you took it, such as 1st Sem SY 2026-2027.")]
    [StringLength(30, ErrorMessage = "This can be at most 30 characters.")]
    public string TermTaken { get; set; } = "";             // term_taken  VARCHAR(30) NOT NULL

    [StringLength(1000, ErrorMessage = "The description can be at most 1000 characters.")]
    public string? Description { get; set; }                // description TEXT, may be NULL

    // Not a column: worked out by LEFT JOIN + COUNT(project_id), so a course with no projects shows 0.
    public long ProjectCount { get; set; }
}