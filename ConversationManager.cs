using System.Text.Json;
using Microsoft.Extensions.AI;

namespace BluelBerry;

/// <summary>Manages conversation persistence and loading from .bb-history folder.</summary>
public class ConversationManager
{
    private const string HistoryFolder = ".bb-history";
    
    /// <summary>Loads the most recent conversation snapshot, excluding quota-exceeded files.</summary>
    public List<ChatMessage> LoadLatestConversation(string systemPrompt)
    {
        try
        {
            if (!Directory.Exists(HistoryFolder))
                return CreateNewConversation(systemPrompt);

            var files = Directory.GetFiles(HistoryFolder, "bb-*.json")
                .Where(p => !Path.GetFileName(p).StartsWith("bb-quota-exceeded-"))
                .OrderByDescending(p => new FileInfo(p).LastWriteTimeUtc)
                .ToArray();

            if (files.Length == 0)
                return CreateNewConversation(systemPrompt);

            var latest = files[0];
            var json = File.ReadAllText(latest);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var msgs = JsonSerializer.Deserialize<List<ChatMessage>>(json, options);

            if (msgs == null || msgs.Count == 0)
                return CreateNewConversation(systemPrompt);

            // Ensure system prompt is present and current
            if (msgs[0].Role != ChatRole.System)
                msgs.Insert(0, new ChatMessage(ChatRole.System, systemPrompt));
            else
                msgs[0] = new ChatMessage(ChatRole.System, systemPrompt);

            Console.WriteLine($"🔄 Loaded conversation snapshot from '{Path.GetFileName(latest)}' ({msgs.Count} messages).");
            DisplayConversation(msgs);
            return msgs;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"⚠️ Failed to load conversation history: {ex.Message}");
            Console.ResetColor();
            return CreateNewConversation(systemPrompt);
        }
    }

    /// <summary>Saves conversation snapshot after each turn.</summary>
    public void SaveConversationSnapshot(List<ChatMessage> messages)
    {
        EnsureHistoryDirectory();
        var json = JsonSerializer.Serialize(messages);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        File.WriteAllText(Path.Combine(HistoryFolder, $"bb-{timestamp}.json"), json);
    }

    /// <summary>Saves conversation before clearing with special prefix.</summary>
    public void SavePreClearSnapshot(List<ChatMessage> messages)
    {
        if (messages.Count <= 1) return;
        
        EnsureHistoryDirectory();
        var json = JsonSerializer.Serialize(messages);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var filename = $"bb-pre-clear-{timestamp}.json";
        File.WriteAllText(Path.Combine(HistoryFolder, filename), json);
        Console.WriteLine($"💾 Conversation saved before clearing to {HistoryFolder}/{filename}");
    }

    /// <summary>Saves quota exceeded snapshot and exits.</summary>
    public void SaveQuotaExceededSnapshot(List<ChatMessage> messages, string errorMessage)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\n🚨 DAILY TOKEN QUOTA EXCEEDED 🚨");
        Console.WriteLine($"Message: {errorMessage}");
        Console.ResetColor();

        EnsureHistoryDirectory();
        var json = JsonSerializer.Serialize(messages);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        File.WriteAllText(Path.Combine(HistoryFolder, $"bb-quota-exceeded-{timestamp}.json"), json);
        Environment.Exit(1);
    }

    /// <summary>Displays the loaded conversation in a readable format.</summary>
    private static void DisplayConversation(List<ChatMessage> messages)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("📜 LOADED CONVERSATION HISTORY");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        Console.ResetColor();

        for (int i = 0; i < messages.Count; i++)
        {
            var message = messages[i];
            
            // Skip system message for display (first message)
            if (i == 0 && message.Role == ChatRole.System)
                continue;

            Console.WriteLine();
            
            // Display role with appropriate color
            if (message.Role == ChatRole.User)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("👤 USER:");
            }
            else if (message.Role == ChatRole.Assistant)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("🤖 ASSISTANT:");
            }
            else if (message.Role == ChatRole.Tool)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("🔧 TOOL:");
            }
            Console.ResetColor();

            // Display message content with proper formatting
            var content = message.Text ?? "";
            if (!string.IsNullOrEmpty(content))
            {
                // Add indentation for readability
                var lines = content.Split('\n');
                foreach (var line in lines)
                {
                    Console.WriteLine($"  {line}");
                }
            }
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        Console.WriteLine("📝 END OF LOADED CONVERSATION");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════");
        Console.ResetColor();
        Console.WriteLine();
    }

    private static List<ChatMessage> CreateNewConversation(string systemPrompt) => 
        new() { new ChatMessage(ChatRole.System, systemPrompt) };

    private static void EnsureHistoryDirectory()
    {
        if (!Directory.Exists(HistoryFolder))
            Directory.CreateDirectory(HistoryFolder);
    }
}