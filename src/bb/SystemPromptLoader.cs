namespace BlueBerry;

/// <summary>Loads and constructs system prompt from various sources.</summary>
public class SystemPromptLoader
{
    /// <summary>Loads system prompt from file and appends current directory and project context.</summary>
    public static string LoadSystemPrompt()
    {
        var bbDirectory = BlueBerryConstants.Directories.Config;
        var systemPromptPath = BlueBerryConstants.Files.SystemPrompt;

        string baseSystemPrompt;
        try
        {
            if (File.Exists(systemPromptPath))
            {
                baseSystemPrompt = LoadExistingSystemPrompt(systemPromptPath);
            }
            else
            {
                baseSystemPrompt = CreateDefaultSystemPrompt(bbDirectory, systemPromptPath, BlueBerryConstants.DefaultSystemPrompt);
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{BlueBerryConstants.Emojis.Warning} Warning: Failed to read system prompt file at {systemPromptPath}. Using default prompt. Error: {ex.Message}");
            Console.ResetColor();
            baseSystemPrompt = BlueBerryConstants.DefaultSystemPrompt;
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
        if (File.Exists(BlueBerryConstants.ContextFiles.Claude))
        {
            Console.WriteLine($"  Adding {BlueBerryConstants.ContextFiles.Claude} to system prompt.");
            baseSystemPrompt += File.ReadAllText(BlueBerryConstants.ContextFiles.Claude);
        }
        else if (File.Exists(BlueBerryConstants.ContextFiles.Gemini))
        {
            Console.WriteLine($"  Adding {BlueBerryConstants.ContextFiles.Gemini} to system prompt.");
            baseSystemPrompt += File.ReadAllText(BlueBerryConstants.ContextFiles.Gemini);
        }

        Console.WriteLine($"  Sending system prompt of {baseSystemPrompt.Length} chars.");
        return baseSystemPrompt;
    }
}