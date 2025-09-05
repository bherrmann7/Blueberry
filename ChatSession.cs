using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace BluelBerry;

/// <summary>
/// Manages the main chat session REPL (Read-Eval-Print Loop) with streaming responses and error handling.
/// This class orchestrates the conversation flow, handles user input, manages context length, 
/// tracks token usage, and coordinates tool calls via MCP clients.
/// </summary>
public class ChatSession
{
    private readonly IChatClient _chatClient;
    private readonly AppOptions _options;
    private readonly TokenTracker _tokenTracker;
    private readonly ConversationManager _conversationManager;
    private readonly McpClientManager _mcpManager;
    private readonly int _maxTokens;
    private string? _lastFunctionCallName;
    private string? _lastPrompt;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatSession"/> class.
    /// </summary>
    /// <param name="chatClient">The chat client used to communicate with the LLM.</param>
    /// <param name="options">Application options containing model and other settings.</param>
    /// <param name="tokenTracker">Tracks token usage and cost information.</param>
    /// <param name="conversationManager">Manages saving/loading conversation history.</param>
    /// <param name="mcpManager">Manages Model Context Protocol clients and their tools.</param>
    public ChatSession(IChatClient chatClient, AppOptions options, TokenTracker tokenTracker, 
        ConversationManager conversationManager, McpClientManager mcpManager)
    {
        _chatClient = chatClient;
        _options = options;
        _tokenTracker = tokenTracker;
        _conversationManager = conversationManager;
        _mcpManager = mcpManager;
        _maxTokens = EnhancedTokenHelper.GetMaxTokens(options.model);
    }

    /// <summary>
    /// Runs the main interactive REPL session.
    /// This method handles user input, displays responses, tracks usage, and saves conversation snapshots.
    /// It supports special commands like /help, /clear, and !! (repeat last prompt).
    /// </summary>
    /// <param name="messages">The current list of chat messages forming the conversation context.</param>
    /// <param name="baseSystemPrompt">The base system prompt that initializes the conversation context.</param>
    public async Task RunAsync(List<ChatMessage> messages, string baseSystemPrompt)
    {
        Console.WriteLine($"Model: {_options.model} | Max Context: {EnhancedTokenHelper.FormatTokenCount(_maxTokens)} tokens");
        
        // Load command history for enhanced input
        var historyFile = ".bb-command-history";
        EnhancedInputHandler.LoadHistory(historyFile);

        Console.WriteLine("\ud83e\uddd1\ud83c\udffb\u200d\ud83d\udcbb Type /help for enhanced input features (emacs\u2011style editing, multi\u2011line input)");
        Console.WriteLine();

        while (true)
        {
            DisplaySessionHeader(messages);

            var prompt = EnhancedInputHandler.ReadInput();
            if (string.IsNullOrWhiteSpace(prompt)) continue;
            if (prompt == "exit" || prompt == "quit") break;
            if (prompt == "!!") prompt = _lastPrompt ?? "";

            if (await HandleCommand(prompt, messages, baseSystemPrompt))
                continue;

            _lastPrompt = prompt;
            messages.Add(new ChatMessage(ChatRole.User, prompt));

            var updates = await GetStreamingResponseWithRetryAsync(messages);
            var finalContextLength = EnhancedTokenHelper.EstimateTokenCount(messages) +
                                   EnhancedTokenHelper.EstimateTokenCount(string.Join("", updates.Where(u => u.Text != null).Select(u => u.Text)));
            var usage = _tokenTracker.TrackStreamingUsage(updates, _options.model, finalContextLength, _maxTokens);
            _tokenTracker.PrintUsageInfo(usage);
            messages.AddMessages(updates);

            _conversationManager.SaveConversationSnapshot(messages);
            PlayNotificationSound();
        }

        // Save command history on exit
        EnhancedInputHandler.SaveHistory(historyFile);
        PrintFinalSummary();
    }

    /// <summary>
    /// Displays session header information including current context length, total cost, and model name.
    /// Also prints a warning if the context length is approaching the maximum limit.
    /// </summary>
    /// <param name="messages">Current list of chat messages to estimate context length.</param>
    private void DisplaySessionHeader(List<ChatMessage> messages)
    {
        var currentContextEstimate = EnhancedTokenHelper.EstimateTokenCount(messages);
        var summary = _tokenTracker.GetSessionSummary();

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine("------------------------------------------------------------------------------------------");
        Console.WriteLine(
            $"Session: ${summary.TotalCost:F4} | \ud83d\udccb Context: {EnhancedTokenHelper.FormatTokenCount(currentContextEstimate)}/{EnhancedTokenHelper.FormatTokenCount(_maxTokens)} ({(double)currentContextEstimate / _maxTokens:P1}) | model: {_options.model}");
        Console.ResetColor();

        EnhancedTokenHelper.PrintContextWarning(currentContextEstimate, _maxTokens);
    }

    /// <summary>
    /// Handles special commands entered by the user.
    /// Supported commands:
    /// - "summary": Prints token usage summary
    /// - "/help": Shows help for enhanced input features
    /// - "/clear": Clears conversation history and resets context
    /// </summary>
    /// <param name="prompt">The user input/command to handle.</param>
    /// <param name="messages">The current conversation messages.</param>
    /// <param name="baseSystemPrompt">The base system prompt for resetting context.</param>
    /// <returns>True if a command was handled, false otherwise.</returns>
    private Task<bool> HandleCommand(string prompt, List<ChatMessage> messages, string baseSystemPrompt)
    {
        switch (prompt)
        {
            case "summary":
                _tokenTracker.PrintSessionSummary();
                return Task.FromResult(true);

            case "/help":
                EnhancedInputHandler.ShowHelp();
                return Task.FromResult(true);

            case "/clear":
                _conversationManager.SavePreClearSnapshot(messages);
                messages.Clear();
                messages.Add(new ChatMessage(ChatRole.System, baseSystemPrompt));
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\ud83d\udfe2 Conversation history cleared! Context reset to system prompt only.");
                Console.WriteLine(
                    $"\ud83d\udccb Context: {EnhancedTokenHelper.FormatTokenCount(EnhancedTokenHelper.EstimateTokenCount(messages))}/{EnhancedTokenHelper.FormatTokenCount(_maxTokens)} tokens");
                Console.ResetColor();
                return Task.FromResult(true);

            default:
                return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Gets streaming response from the chat client with retry logic for rate limiting.
    /// Handles both successful responses and different types of errors (quota exceeded, rate limited).
    /// </summary>
    /// <param name="messages">Messages to send as context to the LLM.</param>
    /// <returns>List of chat response updates from the LLM.</returns>
    private async Task<List<ChatResponseUpdate>> GetStreamingResponseWithRetryAsync(List<ChatMessage> messages)
    {
        var maxRetries = 5;
        var retryDelaySeconds = 1;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var updates = new List<ChatResponseUpdate>();
                await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, new ChatOptions { Tools = [.. _mcpManager.Tools] }))
                {
                    HandleStreamingUpdate(update);
                    updates.Add(update);
                }

                Console.WriteLine();
                return updates;
            }
            catch (Exception ex)
            {
                if (IsQuotaExceeded(ex))
                {
                    _conversationManager.SaveQuotaExceededSnapshot(messages, ex.Message);
                    return new List<ChatResponseUpdate>(); // Never reached due to Environment.Exit
                }

                if (IsRateLimited(ex))
                {
                    RateLimitHelper.LogRateLimit(attempt, retryDelaySeconds);
                    await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
                    retryDelaySeconds *= 2;
                    continue;
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n\ud83c\udf4c Unexpected error: {ex.Message}");
                Console.ResetColor();
                break;
            }
        }

        return new List<ChatResponseUpdate>();
    }

    /// <summary>
    /// Handles individual streaming updates from the chat client.
    /// Processes function calls and results, and displays text content to the console.
    /// </summary>
    /// <param name="update">A single chat response update from the streaming response.</param>
    private void HandleStreamingUpdate(ChatResponseUpdate update)
    {
        if (update.Contents != null)
        {
            foreach (var content in update.Contents)
            {
                if (content is FunctionCallContent call)
                {
                    _lastFunctionCallName = call.Name;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\n\ud83d\udee0 Tool Call: {call.Name}");
                    if (call.Arguments != null && call.Arguments.Count > 0)
                    {
                        var argsJson = JsonSerializer.Serialize(call.Arguments, new JsonSerializerOptions { WriteIndented = true });
                        var indented = argsJson.Replace("\n", "\n    ");
                        Console.WriteLine($"    Arguments: {indented}");
                    }
                    Console.ResetColor();
                }
                else if (content is FunctionResultContent result)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\ud83d\udd04 Tool Result" + (_lastFunctionCallName != null ? $" for {_lastFunctionCallName}" : "") + ":");
                    if (_lastFunctionCallName == "read_file" || _lastFunctionCallName == "write_file")
                        Console.WriteLine("    Operation completed.");
                    else if (result.Result != null)
                        DisplayToolResult(result.Result);

                    Console.ResetColor();
                    Console.WriteLine();
                    _lastFunctionCallName = null;
                }
            }
        }

        Console.Write(Utils.CleanEscapedQuotes(update.ToString()));
    }

    /// <summary>
    /// Displays the result of a tool call in a formatted manner.
    /// Attempts to parse the result as JSON for pretty printing, falls back to string display if parsing fails.
    /// </summary>
    /// <param name="result">The raw result object from the tool call.</param>
    private static void DisplayToolResult(object result)
    {
        var rs = result.ToString();
        if (!string.IsNullOrEmpty(rs))
        {
            // Clean up the text: replace unicode quotes, remove newlines, limit length
            var cleaned = rs.Replace("\\u0022", "\"")
                           .Replace("\n", " ")
                           .Replace("\r", " ")
                           .Replace("  ", " ")  // Remove double spaces
                           .Trim();
            
            // Limit to 100 characters
            if (cleaned.Length > 100)
                cleaned = cleaned.Substring(0, 97) + "...";
                
            Console.WriteLine($"    Result: {cleaned}");
        }
    }

    /// <summary>
    /// Determines if an exception indicates that the token quota has been exceeded.
    /// Checks for specific error messages or HTTP 429 status codes.
    /// </summary>
    /// <param name="ex">The exception to check.</param>
    /// <returns>True if the exception indicates token quota exceeded, false otherwise.</returns>
    private static bool IsQuotaExceeded(Exception ex)
    {
        if (ex.Message?.Contains("token_quota_exceeded", StringComparison.OrdinalIgnoreCase) == true)
            return true;

        if (ex is HttpRequestException http && http.StatusCode == HttpStatusCode.TooManyRequests)
            return true;

        return false;
    }

    /// <summary>
    /// Determines if an exception indicates rate limiting.
    /// Checks for HTTP 429 status codes or error messages containing "429".
    /// </summary>
    /// <param name="ex">The exception to check.</param>
    /// <returns>True if the exception indicates rate limiting, false otherwise.</returns>
    private static bool IsRateLimited(Exception ex)
    {
        return ex.Message?.Contains("429") == true ||
               (ex is HttpRequestException http && http.StatusCode == HttpStatusCode.TooManyRequests);
    }

    /// <summary>
    /// Plays a notification sound to indicate that the LLM has finished responding.
    /// Uses Console.Beep on Windows or afplay on macOS.
    /// </summary>
    private static void PlayNotificationSound()
    {
        if (OperatingSystem.IsWindows())
            Console.Beep();
        else
            try
            {
                Process.Start("afplay", "/System/Library/Sounds/Sosumi.aiff");
            }
            catch
            {
                // Ignore audio errors
            }
    }

    /// <summary>
    /// Prints the final session summary including total cost and token usage statistics.
    /// Also saves a detailed session report to a JSON file.
    /// </summary>
    private void PrintFinalSummary()
    {
        Console.WriteLine("\n" + "=".PadRight(80, '='));
        _tokenTracker.PrintSessionSummary();
        _tokenTracker.SaveSessionReport($".bb-session-final-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.json");
    }
}