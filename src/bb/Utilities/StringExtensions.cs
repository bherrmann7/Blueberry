using System.Text.RegularExpressions;

namespace BlueBerry;

/// <summary>
/// String utility extensions for text processing in Blue Berry.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Cleans escaped quotes from strings, replacing \" with "
    /// This is useful for cleaning up LLM responses that may contain escaped quotes.
    /// </summary>
    public static string CleanEscapedQuotes(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return Regex.Replace(input, "\\\"", "\"");
    }
}
