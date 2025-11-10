namespace BlueBerry;

/// <summary>
/// Centralized constants for Blue Berry configuration, commands, and file paths.
/// This makes the codebase more maintainable and easier to understand.
/// </summary>
public static class BlueBerryConstants
{
    /// <summary>Directory paths for Blue Berry configuration and history.</summary>
    public static class Directories
    {
        public static readonly string UserHome =
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        public static readonly string Config =
            Path.Combine(UserHome, ".bb");

        public static readonly string History =
            Path.Combine(UserHome, ".bb-history");
    }

    /// <summary>File paths for configuration and history files.</summary>
    public static class Files
    {
        public static readonly string CommandHistory = ".bb-command-history";

        public static readonly string SystemPrompt =
            Path.Combine(Directories.Config, "system-prompt.txt");

        public static readonly string McpConfig =
            Path.Combine(Directories.Config, "mcp.json");

        public static readonly string ModelsConfig =
            Path.Combine(Directories.Config, "models.json");
    }

    /// <summary>User commands that can be entered in the REPL.</summary>
    public static class Commands
    {
        public const string Exit = "exit";
        public const string Quit = "quit";
        public const string Help = "/help";
        public const string Clear = "/clear";
        public const string Resume = "/resume";
        public const string Summary = "summary";
        public const string RepeatLast = "!!";

        /// <summary>Gets all available commands for auto-completion.</summary>
        public static readonly string[] AllCommands =
        {
            Help, Clear, Resume, Summary, RepeatLast, Exit, Quit
        };
    }

    /// <summary>Special command arguments for model and MCP listing.</summary>
    public static class SpecialArguments
    {
        public static readonly string[] ModelCommands = { "model", "m", "models" };
        public const string McpCommand = "mcp";
    }

    /// <summary>File name prefixes for different snapshot types.</summary>
    public static class SnapshotPrefixes
    {
        public const string Conversation = "bb-";
        public const string QuotaExceeded = "bb-quota-exceeded-";
        public const string PreClear = "bb-pre-clear-";
        public const string SessionFinal = "bb-session-final-";
        public const string HttpRequest = "bb-req-";
        public const string HttpResponse = "bb-resp-";
    }

    /// <summary>Project context file names that Blue Berry looks for.</summary>
    public static class ContextFiles
    {
        public const string Claude = "CLAUDE.md";
        public const string Gemini = "GEMINI.md";
    }

    /// <summary>Console emojis used throughout the application.</summary>
    public static class Emojis
    {
        public const string Blueberry = "🫐";
        public const string Tool = "🔧";
        public const string ToolResult = "🔄";
        public const string Metrics = "📊";
        public const string Numbers = "🔢";
        public const string Money = "💰";
        public const string Coin = "🪙";
        public const string Success = "✅";
        public const string Error = "❌";
        public const string Warning = "⚠️";
        public const string Info = "💡";
        public const string User = "👤";
        public const string Assistant = "🤖";
        public const string Green = "🟢";
        public const string Refresh = "🔄";
        public const string Save = "💾";
        public const string Alert = "🚨";
        public const string Notebook = "📋";
        public const string Lightning = "⚡";
    }

    /// <summary>Default system prompt for the assistant.</summary>
    public const string DefaultSystemPrompt = """
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
}
