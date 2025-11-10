using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace BlueBerry;

/// <summary>
/// Manages the main agent loop following the ReAct (Reason + Act) pattern.
///
/// AGENT LOOP ARCHITECTURE:
/// ========================
///
/// An AI agent is a program that autonomously achieves goals by:
/// 1. PERCEIVE - Receiving input from the user
/// 2. REASON - Using an LLM to decide what to do
/// 3. ACT - Executing tools to gather information or take actions
/// 4. RESPOND - Providing output back to the user
///
/// THE ReAct PATTERN:
/// ==================
///
/// This implementation uses the "ReAct" (Reasoning + Acting) pattern:
///
/// User: "What files are in my home directory?"
///   ↓
/// REASON: LLM thinks "I need to list directory contents"
///   ↓
/// ACT: Calls list_directory("/home/user") tool via MCP
///   ↓
/// REASON: LLM thinks "Now I have the list, I can answer"
///   ↓
/// RESPOND: "Your home directory contains: file1.txt, file2.txt..."
///
/// HOW IT WORKS:
/// =============
///
/// 1. The LLM receives:
///    - Full conversation history (system prompt + all messages)
///    - List of available tools (from MCP servers)
///    - Current user prompt
///
/// 2. The LLM can respond with either:
///    - Text (it's done reasoning and ready to answer)
///    - Function calls (it needs to use tools to proceed)
///
/// 3. When function calls are made:
///    - Blue Berry executes them via MCP
///    - Results are added back to conversation history
///    - Process loops back to step 1
///
/// 4. This continues until the LLM responds with text instead of function calls
///
/// THE TOKEN BURNING PROBLEM:
/// ==========================
///
/// ⚠️ WARNING: This implementation is intentionally simple but INEFFICIENT!
///
/// Every request sends the ENTIRE conversation history to the LLM.
/// For a 10-turn conversation:
///   - Turn 1: Send 100 tokens
///   - Turn 2: Send 200 tokens (100 old + 100 new)
///   - Turn 3: Send 300 tokens (200 old + 100 new)
///   - ...
///   - Total: ~5,500 tokens sent for only 1,000 tokens of actual content!
///
/// This makes the agent expensive and slow as conversations grow.
///
/// Production agents solve this with:
/// - **Conversation Summarization**: Compress old turns into summaries
/// - **Sliding Windows**: Keep only recent N messages
/// - **RAG (Retrieval-Augmented Generation)**: Store history in vector DB, retrieve relevant parts
/// - **Semantic Caching**: Cache repeated context at the LLM provider level
///
/// Blue Berry prioritizes CLARITY over EFFICIENCY - it's designed for learning,
/// not production use. Understanding this naive approach helps you appreciate
/// why production agents need sophisticated memory management.
///
/// KEY COMPONENTS:
/// ===============
///
/// - IChatClient: Abstraction for talking to any LLM (OpenAI, Anthropic, etc.)
/// - IMcpClientManager: Discovers and manages tools from MCP servers
/// - IConversationManager: Saves/loads conversation history for persistence
/// - ITokenTracker: Monitors token usage and costs
///
/// FLOW THROUGH THIS CLASS:
/// =========================
///
/// RunAsync() → Main REPL loop
///   → ReadUserInput()
///   → GetStreamingResponseWithRetryAsync() → Calls LLM with history + tools
///     → HandleStreamingUpdate() → Processes LLM response chunks
///       → Tool calls executed automatically by .UseFunctionInvocation()
///   → Save conversation state
///   → Repeat
///
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

    /// <summary>Initializes a new ChatSession with required dependencies.</summary>
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

    /// <summary>Runs the main interactive REPL session with commands like /help, /clear, /resume, and !!.</summary>
    public async Task RunAsync(List<ChatMessage> messages, string baseSystemPrompt)
    {
        Console.WriteLine($"  Model: {_options.model} | Max Context: {EnhancedTokenHelper.FormatTokenCount(_maxTokens)} tokens");
        
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
            if (prompt == "/exit" || prompt == "/quit") break;
            if (prompt == "!!") prompt = _lastPrompt ?? "";

            if (await HandleCommand(prompt, messages, baseSystemPrompt))
                continue;

            _lastPrompt = prompt;
            messages.Add(new ChatMessage(ChatRole.User, prompt));

            var updates = await GetStreamingResponseWithRetryAsync(messages);
            var finalContextLength = EnhancedTokenHelper.EstimateTokenCount(messages) +
                                   EnhancedTokenHelper.EstimateTokenCount(string.Join("", updates.Where(u => u.Text != null).Select(u => u.Text)));
            _tokenTracker.TrackStreamingUsage(updates, _options.model, finalContextLength, _maxTokens);
            messages.AddMessages(updates);

            _conversationManager.SaveConversationSnapshot(messages);
            PlayNotificationSound();
        }

        // Save command history on exit
        EnhancedInputHandler.SaveHistory(historyFile);
        PrintFinalSummary();
    }

    /// <summary>Displays session header with context length, cost, and model info.</summary>
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

    /// <summary>Handles special commands: summary, /help, /clear, /resume.</summary>
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

            case "/resume":
                var loaded = _conversationManager.LoadLatestConversation(baseSystemPrompt);
                messages.Clear();
                messages.AddRange(loaded);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ Resumed last conversation.");
                Console.WriteLine(
                    $"\ud83d\udccb Context: {EnhancedTokenHelper.FormatTokenCount(EnhancedTokenHelper.EstimateTokenCount(messages))}/{EnhancedTokenHelper.FormatTokenCount(_maxTokens)} tokens");
                Console.ResetColor();
                return Task.FromResult(true);

            default:
                return Task.FromResult(false);
        }
    }

    /// <summary>Gets streaming response with retry logic for rate limiting and quota errors.</summary>
    private async Task<List<ChatResponseUpdate>> GetStreamingResponseWithRetryAsync(List<ChatMessage> messages)
    {
        var maxRetries = 50;
        var retryDelaySeconds = 1;

        var attempt = 1;
        for (; attempt <= maxRetries; attempt++)
        {
            try
            {
                var updates = new List<ChatResponseUpdate>();

                // ═══════════════════════════════════════════════════════════════
                // 🎯 AGENT CORE: This is where the magic happens!
                // ═══════════════════════════════════════════════════════════════
                //
                // We send to the LLM:
                //   1. `messages` - Full conversation history (the "memory")
                //   2. `Tools` - Available MCP tools (the "capabilities")
                //
                // The LLM will decide to either:
                //   - Return text (done reasoning, ready to respond)
                //   - Call functions (needs to use tools to proceed)
                //
                // The .UseFunctionInvocation() middleware (configured in ChatClientFactory)
                // automatically:
                //   - Detects function calls in the LLM response
                //   - Executes them via MCP
                //   - Adds results back to the conversation
                //   - Loops back to the LLM until it responds with text
                //
                // This is the "ReAct loop" in action!
                // ═══════════════════════════════════════════════════════════════

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

        if (attempt == maxRetries)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n\ud83c\udf4c Boo - I tried {maxRetries} and I got nada. Going back to the prompt.");
            Console.ResetColor();
        }


        return new List<ChatResponseUpdate>();
    }

    /// <summary>Handles streaming updates, processes function calls and displays content.</summary>
    private void HandleStreamingUpdate(ChatResponseUpdate update)
    {
        // Each "update" is a chunk from the streaming LLM response.
        // Updates can contain:
        //   - Text content (partial response text)
        //   - Function calls (LLM requesting tool execution)
        //   - Function results (tool execution completed)
        //   - Usage metadata (token counts)

        if (update.Contents != null)
        {
            foreach (var content in update.Contents)
            {
                // 🔧 TOOL CALL: The LLM has decided it needs to use a tool
                // This demonstrates the "Act" phase of ReAct
                if (content is FunctionCallContent call)
                {
                    _lastFunctionCallName = call.Name;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    var summary = ToolLogger.SummarizeFunctionCall(call);
                    Console.WriteLine($"\n{BlueBerryConstants.Emojis.Tool} Tool Call: {summary}");
                    Console.ResetColor();
                }
                // ✅ TOOL RESULT: The tool has finished executing
                // The result will be added to conversation history and sent back to the LLM
                else if (content is FunctionResultContent result)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    var summary = ToolLogger.SummarizeFunctionResult(result, _lastFunctionCallName);
                    Console.WriteLine($"{BlueBerryConstants.Emojis.ToolResult} Tool Result: {summary}");
                    Console.ResetColor();
                    _lastFunctionCallName = null;
                }
            }
        }

        // 📝 TEXT CONTENT: Stream the LLM's text response to the user in real-time
        // This gives immediate feedback and creates a more interactive experience
        Console.Write(update.ToString().CleanEscapedQuotes());
    }

    /// <summary>Displays tool call results in a formatted manner.</summary>
    private static void DisplayToolResult(object result)
    {
        var rs = result.ToString();
        if (!string.IsNullOrEmpty(rs))
        {
            // Clean up the text: replace unicode quotes, remove newlines, limit length
            var cleaned = rs.Replace("\\\"", "\"")
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

    /// <summary>Determines if exception indicates token quota exceeded.</summary>
    private static bool IsQuotaExceeded(Exception ex)
    {
        if (ex.Message?.Contains("token_quota_exceeded", StringComparison.OrdinalIgnoreCase) == true)
            return true;

        if (ex is HttpRequestException http && http.StatusCode == HttpStatusCode.TooManyRequests)
            return true;

        return false;
    }

    /// <summary>Determines if exception indicates rate limiting.</summary>
    private static bool IsRateLimited(Exception ex)
    {
        return ex.Message?.Contains("429") == true ||
               (ex is HttpRequestException http && http.StatusCode == HttpStatusCode.TooManyRequests);
    }

    /// <summary>Plays notification sound when LLM finishes responding.</summary>
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

    /// <summary>Prints final session summary and saves report to JSON.</summary>
    private void PrintFinalSummary()
    {
        Console.WriteLine("\n" + "=".PadRight(80, '='));
        _tokenTracker.PrintSessionSummary();
        _tokenTracker.SaveSessionReport($".bb-session-final-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.json");
    }
}
