using System.Text;

namespace BlueBerry;

/// <summary>
/// Provides enhanced multi-line input with history and emacs-style editing.
/// </summary>
public static class EnhancedInputHandler
{
    private static readonly List<string> _history = new();


    static EnhancedInputHandler()
    {
        // Set up ReadLine with emacs-style key bindings
        ReadLine.HistoryEnabled = true;
        ReadLine.AutoCompletionHandler = new AutoCompletionHandler();
    }

    /// <summary>Reads multi-line input. Submit with '.' on empty line or Ctrl+D.</summary>
    public static string? ReadInput(string prompt = "🫐 ")
    {
        Console.Write(prompt);

        // Use the new multi-line input mode by default
        // This way Enter always adds a new line, and Ctrl+Enter (or just . for now) submits
        return ReadMultilineInputEnhanced();
    }

    private static string ReadMultilineInputEnhanced()
    {
        var sb = new StringBuilder();
        Console.WriteLine("(multi-line mode - Ctrl+D or '.' on empty line to submit)");

        while (true)
        {
            Console.Write(">> ");
            var line = ReadLine.Read() ?? "";

            // Check for submission commands - either empty line with "." or Ctrl+D
            if (line == "." && sb.Length > 0)
            {
                // Remove the last line if it's just the "." submission command
                var result = sb.ToString().TrimEnd();
                if (result.EndsWith(Environment.NewLine + ".", StringComparison.Ordinal))
                    result = result.Substring(0, result.Length - (Environment.NewLine + ".").Length);
                else if (result.EndsWith("." + Environment.NewLine, StringComparison.Ordinal))
                    result = result.Substring(0, result.Length - ("." + Environment.NewLine).Length);
                else if (result.EndsWith(".")) result = result.Substring(0, result.Length - 1);

                // Add to history if not empty and not a command
                if (!string.IsNullOrWhiteSpace(result) && !result.StartsWith("/")) AddToHistory(result);

                return result;
            }

            // Handle case where user enters "." on an empty line (without any previous content)
            if (line == "." && sb.Length == 0) return "";

            // Ctrl+D handling - only submit on actual null (EOF), not empty string
            if (line == null)
            {
                // Actual Ctrl+D detected (EOF)
                var result = sb.ToString().TrimEnd();

                // Add to history if not empty and not a command
                if (!string.IsNullOrWhiteSpace(result) && !result.StartsWith("/")) AddToHistory(result);

                return result;
            }

            sb.AppendLine(line);
        }
    }

    private static void AddToHistory(string input)
    {
        if (_history.Count == 0 || _history[^1] != input)
        {
            _history.Add(input);
            ReadLine.AddHistory(input);
        }
    }

    /// <summary>Loads command history from file.</summary>
    public static void LoadHistory(string historyFile)
    {
        try
        {
            if (File.Exists(historyFile))
            {
                var lines = File.ReadAllLines(historyFile);
                foreach (var line in lines)
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        _history.Add(line);
                        ReadLine.AddHistory(line);
                    }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not load history from {historyFile}: {ex.Message}");
        }
    }

    /// <summary>Saves command history to file.</summary>
    public static void SaveHistory(string historyFile)
    {
        try
        {
            File.WriteAllLines(historyFile, _history);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not save history to {historyFile}: {ex.Message}");
        }
    }

    /// <summary>Displays help for input features and commands.</summary>
    public static void ShowHelp()
    {
        Console.WriteLine("Enhanced Input Features:");
        Console.WriteLine("  Emacs-style editing:");
        Console.WriteLine("    Ctrl+A    - Beginning of line");
        Console.WriteLine("    Ctrl+E    - End of line");
        Console.WriteLine("    Ctrl+F    - Forward character");
        Console.WriteLine("    Ctrl+B    - Backward character");
        Console.WriteLine("    Alt+F     - Forward word");
        Console.WriteLine("    Alt+B     - Backward word");
        Console.WriteLine("    Ctrl+D    - Delete character");
        Console.WriteLine("    Ctrl+K    - Kill to end of line");
        Console.WriteLine("    Ctrl+U    - Kill entire line");
        Console.WriteLine("    Ctrl+Y    - Yank (paste)");
        Console.WriteLine("    Ctrl+P    - Previous history");
        Console.WriteLine("    Ctrl+N    - Next history");
        Console.WriteLine("  Multi-line input (updated behavior):");
        Console.WriteLine("    Enter     - Add new line");
        Console.WriteLine("    Ctrl+D or '.' on empty line - Submit input");
        Console.WriteLine("  Commands:");
        Console.WriteLine("    /clear    - Clear conversation history");
        Console.WriteLine("    /resume   - Load last saved conversation");
        Console.WriteLine("    /help     - Show this help");
        Console.WriteLine("    summary   - Show session summary");
        Console.WriteLine("    !!        - Repeat last prompt");
        Console.WriteLine("    exit/quit - Exit Blue Berry");
    }
}

/// <summary>Handles auto-completion for commands.</summary>
public class AutoCompletionHandler : IAutoCompleteHandler
{
    private static readonly string[] Commands =
    {
        "/clear", "/resume", "/help", "summary", "exit", "quit", "!!"
    };

    public char[] Separators { get; set; } = { ' ', '.', '/' };

    public string[] GetSuggestions(string text, int index)
    {
        var suggestions = new List<string>();

        // Command completion
        if (text.StartsWith("/") || text.StartsWith("s") || text.StartsWith("e") || text.StartsWith("q") || text.StartsWith("!"))
            foreach (var cmd in Commands)
                if (cmd.StartsWith(text, StringComparison.OrdinalIgnoreCase))
                    suggestions.Add(cmd);

        return suggestions.ToArray();
    }
}
