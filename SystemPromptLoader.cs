namespace BluelBerry;

/// <summary>Loads and constructs system prompt from various sources.</summary>
public class SystemPromptLoader
{
    /// <summary>Loads system prompt from file and appends current directory and project context.</summary>
    public static string LoadSystemPrompt()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var systemPromptPath = Path.Combine(homeDirectory, ".bb", "system-prompt.txt");
        var baseSystemPrompt = "You are a helpful AI assistant."; // Default fallback

        try
        {
            if (File.Exists(systemPromptPath))
            {
                baseSystemPrompt = File.ReadAllText(systemPromptPath);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠️ Warning: System prompt file not found at {systemPromptPath}. Using default prompt.");
                Console.ResetColor();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"⚠️ Warning: Failed to read system prompt file at {systemPromptPath}. Using default prompt. Error: {ex.Message}");
            Console.ResetColor();
        }

        var currentDirectory = Environment.CurrentDirectory;
        baseSystemPrompt += $"\n\nCurrent working directory: {currentDirectory}\n";

        // Add project-specific context files
        if (File.Exists("CLAUDE.md"))
        {
            Console.WriteLine("Adding CLAUDE.md to system prompt.");
            baseSystemPrompt += File.ReadAllText("CLAUDE.md");
        }
        else if (File.Exists("GEMINI.md"))
        {
            Console.WriteLine("Adding GEMINI.md to system prompt.");
            baseSystemPrompt += File.ReadAllText("GEMINI.md");
        }

        Console.WriteLine($"Sending system prompt of {baseSystemPrompt.Length} chars.");
        return baseSystemPrompt;
    }
}