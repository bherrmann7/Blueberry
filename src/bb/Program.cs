using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.AI;
using BluelBerry;

internal static class Utils
{
    // Method for cleaning escaped quotes
    public static string CleanEscapedQuotes(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return System.Text.RegularExpressions.Regex.Replace(input, "\\\"", "\"");
    }
}

internal static class RateLimitHelper
{
    // Logs a rate limit event and retry delay
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
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;
        
        Console.WriteLine("Welcome to Blue Berry 🫐");

        if (args.Contains("--help") || args.Contains("-h") || args.Contains("-?"))
        {
            Console.WriteLine(AppOptionsParser.Usage<AppOptions>("bb"));
            return 0;
        }

        var options = AppOptionsParser.Parse<AppOptions>(args);

        var (chatClient, samplingClient, loggerFactory, tokenTracker) = ChatClientFactory.Create(options);
        var conversationManager = new ConversationManager();
        var mcpManager = new McpClientManager();
        
        try
        {
            await mcpManager.InitializeAsync(samplingClient);

            var baseSystemPrompt = SystemPromptLoader.LoadSystemPrompt();
            List<ChatMessage> messages = new List<ChatMessage> { new ChatMessage(ChatRole.System, baseSystemPrompt) };
            
            var session = new ChatSession(chatClient, options, tokenTracker, conversationManager, mcpManager);
            await session.RunAsync(messages, baseSystemPrompt);
        }
        finally
        {
            mcpManager.Dispose();
            loggerFactory.Dispose();
            tokenTracker.Dispose();
        }

        return 0;
    }
}
