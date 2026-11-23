namespace portfolio.Models;

// The three regular expressions of the portfolio, written ONCE.
// The same text is used by the model attributes (server), by the browser scripts (client)
// and by Regex.IsMatch in the code, so the three can never disagree.
public static class Patterns
{
    // 1. One image URL: http or https, then no spaces, then a picture extension
    //    (and optionally a ?query such as ?text=Hello). Matched without caring about capitals.
    public const string ImageUrl = @"^https?://\S+\.(jpg|jpeg|png|gif|webp)(\?\S*)?$";

    // 2. A YouTube link: watch?v=ID, youtu.be/ID or shorts/ID, where ID is 11 letters, digits, _ or -.
    public const string VideoUrl =
        @"^(https?://)?(www\.)?(youtube\.com/(watch\?v=|shorts/)|youtu\.be/)[A-Za-z0-9_-]{11}([&?]\S*)?$";

    // 3. A course code: 2 to 6 capital letters, an optional space or dash, 1 to 4 digits, an optional capital.
    //    Passes: IT 205, CC101, NSTP-1, GE 4A.   Fails: it205, 205, IT.
    public const string CourseCode = @"^[A-Z]{2,6}[ -]?[0-9]{1,4}[A-Z]?$";
}