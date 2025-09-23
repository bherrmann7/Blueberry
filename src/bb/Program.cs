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

        // Check if the first argument is "model", "m", or "models" to list all available models
        if (args.Length > 0 && (args[0].Equals("model", StringComparison.OrdinalIgnoreCase) ||
                                args[0].Equals("m", StringComparison.OrdinalIgnoreCase) ||
                                args[0].Equals("models", StringComparison.OrdinalIgnoreCase)))
        {
            var listModelManager = new ModelManager();
            DisplayModels(listModelManager.Models);
            return 0;
        }

        if (args.Contains("--help") || args.Contains("-h") || args.Contains("/?"))
        {
            Console.WriteLine(AppOptionsParser.Usage<AppOptions>("bb"));
            return 0;
        }

        // Initialize the model manager once
        var modelManager = new ModelManager();
        AppOptions options;

        // Check if first argument is a model short name
        if (args.Length > 0 && !args[0].StartsWith("-"))
        {
            // First argument is treated as a model short name
            var model = modelManager.GetModelByShortName(args[0]);
            
            if (model == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: Model with short name '{args[0]}' not found in models.json");
                Console.ResetColor();
                Console.WriteLine("Use 'bb models' to see available models.");
                return 1;
            }

            // Create options from the model configuration
            // Use the actual model name for API calls but preserve short name for display
            options = new AppOptions(model.Name, model.Endpoint, model.Key, false);
            
            // Display the configuration that will actually be used
            // Use the resolved model name instead of the short name for clarity
            DisplayConfiguration(options);
        }
        else
        {
            // Parse all arguments as normal (this will display the configuration)
            options = AppOptionsParser.Parse<AppOptions>(args);

            // Check if this combination of model, endpoint, and key should be added to models.json
            // We add if the combination doesn't match the defaults and all three are non-empty
            var defaultOptions = new AppOptions();
            
            bool shouldAutoAdd = !string.IsNullOrEmpty(options.model) && 
                                !string.IsNullOrEmpty(options.endpoint) && 
                                !string.IsNullOrEmpty(options.key) &&
                                (options.model != defaultOptions.model || 
                                 options.endpoint != defaultOptions.endpoint || 
                                 options.key != defaultOptions.key);

            if (shouldAutoAdd)
            {
                modelManager.AddModelIfNotExists(options.model, options.endpoint, options.key);
            }
        }

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

    private static void DisplayConfiguration(AppOptions options)
    {
        Console.Write("  Configuration:");
        Console.Write($"   --model: {options.model}");
        Console.Write($"   --endpoint: {options.endpoint}");
        
        // Show key with obfuscation
        var displayKey = options.key.Length > 8 
            ? options.key.Substring(0, Math.Min(4, options.key.Length)) + "..." + options.key.Substring(Math.Max(0, options.key.Length - 4))
            : options.key;
        Console.Write($"   --key: {displayKey}");
        Console.Write($"   --enable-http-logging: {options.enableHttpLogging}");
        Console.WriteLine();
    }

    private static void DisplayModels(List<Model> models)
    {
        if (models == null || models.Count == 0)
        {
            Console.WriteLine("No models found.");
            return;
        }

        // Calculate column widths for proper formatting
        var shortnameWidth = Math.Max("Shortname".Length, models.Max(m => m.ShortName.Length));
        var nameWidth = Math.Max("Model".Length, models.Max(m => m.Name.Length));
        var endpointWidth = Math.Max("Endpoint".Length, models.Max(m => m.Endpoint.Length));

        // Ensure minimum widths for better readability
        shortnameWidth = Math.Max(shortnameWidth, 12);
        nameWidth = Math.Max(nameWidth, 15);
        endpointWidth = Math.Max(endpointWidth, 25);

        // Header
        Console.WriteLine($"{PadRight("Shortname", shortnameWidth)} | {PadRight("Model", nameWidth)} | {"Endpoint"}");
        Console.WriteLine($"{new string('-', shortnameWidth)} | {new string('-', nameWidth)} | {new string('-', endpointWidth)}");

        // Model data
        foreach (var model in models)
        {
            Console.WriteLine($"{PadRight(model.ShortName, shortnameWidth)} | {PadRight(model.Name, nameWidth)} | {model.Endpoint}");
        }
    }

    private static string PadRight(string text, int width)
    {
        if (text.Length >= width)
            return text.Length > width ? text.Substring(0, width) : text;
        return text.PadRight(width);
    }
}