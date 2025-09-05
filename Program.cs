using BluelBerry;

// Helper utility class for cleaning escaped Unicode quotes.
internal static class Utils
{
    public static string CleanEscapedQuotes(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Replace sequences of escaped quotes with a single quote
        return System.Text.RegularExpressions.Regex.Replace(input, @"(\\"")+", "\"");
    }
}

internal static class RateLimitHelper
{
    /// <summary>Logs a rate limit event and retry delay.</summary>
    public static void LogRateLimit(int attempt, int delaySeconds)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"--- Rate limit encountered. Retrying in {delaySeconds}s... (attempt {attempt})");
        Console.ResetColor();
    }
}

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.WriteLine("Welcome to 🫐 Blue Berry 🫐");

        if (args.Contains("--help"))
        {
            Console.WriteLine(AppOptionsParser.Usage<AppOptions>("bb"));
            return 0;
        }

        var options = AppOptionsParser.Parse<AppOptions>(args);
        Console.WriteLine(options);

        // Initialize all services
        var (chatClient, samplingClient, loggerFactory, tokenTracker) = ChatClientFactory.Create(options);
        var conversationManager = new ConversationManager();
        var mcpManager = new McpClientManager();
        
        try
        {
            // Initialize MCP clients and tools
            await mcpManager.InitializeAsync(samplingClient);

            // Load system prompt and conversation history
            var baseSystemPrompt = SystemPromptLoader.LoadSystemPrompt();
            var messages = conversationManager.LoadLatestConversation(baseSystemPrompt);

            // Run the chat session
            var session = new ChatSession(chatClient, options, tokenTracker, conversationManager, mcpManager);
            await session.RunAsync(messages, baseSystemPrompt);
        }
        finally
        {
            // Cleanup resources
            mcpManager.Dispose();
            loggerFactory.Dispose();
            tokenTracker.Dispose();
        }

        return 0;
    }
}