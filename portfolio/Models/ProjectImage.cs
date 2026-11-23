using System.ComponentModel.DataAnnotations;

namespace portfolio.Models;

// One ProjectImage object holds one row of portfolio.project_image.
public class ProjectImage
{
    public long ImageId { get; set; }                       // image_id    BIGINT, the primary key
    public long ProjectId { get; set; }                     // project_id  BIGINT NOT NULL, foreign key

    [Required(ErrorMessage = "Paste the image URL.")]
    [StringLength(500, ErrorMessage = "The URL can be at most 500 characters.")]
    [ImageUrlLines]
    public string ImageUrl { get; set; } = "";              // image_url   VARCHAR(500) NOT NULL, UNIQUE with project_id

    public string? ProjectTitle { get; set; }               // from portfolio.project, for the page heading
}