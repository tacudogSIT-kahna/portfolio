namespace portfolio.Models;

public static class ImageLines
{
    // Turns the text box ("one URL per line") into a clean list: trimmed, no blank lines.
    public static List<string> Split(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<string>();
        }

        return text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                   .ToList();
    }
}