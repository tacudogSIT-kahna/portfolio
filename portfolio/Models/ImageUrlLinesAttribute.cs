using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace portfolio.Models;

// A validation rule for a box of image URLs, one per line (it also works for a single URL).
// SERVER: every line is checked with Regex.IsMatch.
// BROWSER: AddValidation writes data-val-* attributes; wwwroot/js/image-urls.js reads them.
[AttributeUsage(AttributeTargets.Property)]
public class ImageUrlLinesAttribute : ValidationAttribute, IClientModelValidator
{
    private const string DefaultMessage =
        "Each line must be a full image address (http or https) ending in .jpg, .jpeg, .png, .gif or .webp.";

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var lines = ImageLines.Split(value as string);

        for (int i = 0; i < lines.Count; i++)
        {
            if (!Regex.IsMatch(lines[i], Patterns.ImageUrl, RegexOptions.IgnoreCase))
            {
                return new ValidationResult($"Line {i + 1} is not a valid image URL. " + (ErrorMessage ?? DefaultMessage));
            }
        }

        return ValidationResult.Success;        // an empty box is allowed: [Required] decides that
    }

    // The browser half: tell the page which rule to run and with which pattern.
    public void AddValidation(ClientModelValidationContext context)
    {
        Add(context.Attributes, "data-val", "true");
        Add(context.Attributes, "data-val-imageurls", ErrorMessage ?? DefaultMessage);
        Add(context.Attributes, "data-val-imageurls-pattern", Patterns.ImageUrl);
    }

    private static void Add(IDictionary<string, string> attributes, string key, string value)
    {
        if (!attributes.ContainsKey(key))
        {
            attributes.Add(key, value);
        }
    }
}