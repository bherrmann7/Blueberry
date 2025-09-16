namespace BluelBerry;

/// <summary>Loads and constructs system prompt from various sources.</summary>
public class SystemPromptLoader
{
    /// <summary>Loads system prompt from file and appends current directory and project context.</summary>
    public static string LoadSystemPrompt()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var bbDirectory = Path.Combine(homeDirectory, ".bb");
        var systemPromptPath = Path.Combine(bbDirectory, "system-prompt.txt");

        var defaultSystemPrompt = """
You are an expert software engineer and coding assistant. When given a task:

1. Always complete the full implementation yourself
2. Write working, tested code with proper error handling
3. Don't ask for confirmation on standard practices
4. Only ask questions if requirements are genuinely unclear
5. Be thorough - implement edge cases and consider performance
6. Take initiative to suggest improvements when you see opportunities
7. Write complete code implementations rather than partial solutions
8. Make reasonable assumptions about requirements
9. Test your implementations when possible

You should be proactive and autonomous in solving problems. Complete tasks fully rather than handing work back to the user unless you genuinely need clarification.
""";

        string baseSystemPrompt;
        try
        {
            if (File.Exists(systemPromptPath))
            {
                baseSystemPrompt = LoadExistingSystemPrompt(systemPromptPath);
            }
            else
            {
                baseSystemPrompt = CreateDefaultSystemPrompt(bbDirectory, systemPromptPath, defaultSystemPrompt);
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"⚠️ Warning: Failed to read system prompt file at {systemPromptPath}. Using default prompt. Error: {ex.Message}");
            Console.ResetColor();
            baseSystemPrompt = defaultSystemPrompt;
        }

        return BuildFullSystemPrompt(baseSystemPrompt);
    }

    private static string LoadExistingSystemPrompt(string systemPromptPath)
    {
        return File.ReadAllText(systemPromptPath);
    }

    private static string CreateDefaultSystemPrompt(string bbDirectory, string systemPromptPath, string defaultSystemPrompt)
    {
        try
        {
            // Ensure .bb directory exists
            if (!Directory.Exists(bbDirectory))
            {
                Directory.CreateDirectory(bbDirectory);
            }

            // Create default system prompt file
            File.WriteAllText(systemPromptPath, defaultSystemPrompt);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✅ Created default system prompt at {systemPromptPath}");
            Console.WriteLine("   💡 You can edit this file to customize the assistant's behavior.");
            Console.ResetColor();

            return defaultSystemPrompt;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"⚠️ Warning: Failed to create system prompt file at {systemPromptPath}. Error: {ex.Message}");
            Console.ResetColor();
            return defaultSystemPrompt;
        }
    }

    private static string BuildFullSystemPrompt(string baseSystemPrompt)
    {
        var currentDirectory = Environment.CurrentDirectory;
        baseSystemPrompt += $"\n\nCurrent working directory is {currentDirectory}\n";

        // Add project-specific context files
        if (File.Exists("CLAUDE.md"))
        {
            Console.WriteLine("  Adding CLAUDE.md to system prompt.");
            baseSystemPrompt += File.ReadAllText("CLAUDE.md");
        }
        else if (File.Exists("GEMINI.md"))
        {
            Console.WriteLine("  Adding GEMINI.md to system prompt.");
            baseSystemPrompt += File.ReadAllText("GEMINI.md");
        }

        Console.WriteLine($"  Sending system prompt of {baseSystemPrompt.Length} chars.");
        return baseSystemPrompt;
    }
}