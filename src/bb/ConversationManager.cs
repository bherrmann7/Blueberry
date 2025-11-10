using System.Text.Json;
using Microsoft.Extensions.AI;

namespace BlueBerry;

/// <summary>Manages conversation persistence and loading from .bb-history folder.</summary>
public class ConversationManager : IConversationManager
{
    private static readonly string HistoryFolder = BlueBerryConstants.Directories.History;
    
    /// <summary>Loads the most recent conversation snapshot, excluding non-conversation logs.</summary>
    public List<ChatMessage> LoadLatestConversation(string systemPrompt)
    {
        try
        {
            if (!Directory.Exists(HistoryFolder))
                return CreateNewConversation(systemPrompt);

            // Only include actual conversation snapshots. Exclude quota, req/resp logs, and final reports.
            var files = Directory.GetFiles(HistoryFolder, $"{BlueBerryConstants.SnapshotPrefixes.Conversation}*.json")
                .Select(p => new FileInfo(p))
                .Where(fi => !fi.Name.StartsWith(BlueBerryConstants.SnapshotPrefixes.QuotaExceeded, StringComparison.OrdinalIgnoreCase)
                             && !fi.Name.StartsWith(BlueBerryConstants.SnapshotPrefixes.HttpRequest, StringComparison.OrdinalIgnoreCase)
                             && !fi.Name.StartsWith(BlueBerryConstants.SnapshotPrefixes.HttpResponse, StringComparison.OrdinalIgnoreCase)
                             && !fi.Name.StartsWith(BlueBerryConstants.SnapshotPrefixes.SessionFinal, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(fi => fi.LastWriteTimeUtc)
                .Select(fi => fi.FullName)
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
        File.WriteAllText(Path.Combine(HistoryFolder, $"{BlueBerryConstants.SnapshotPrefixes.Conversation}{timestamp}.json"), json);
    }

    /// <summary>Saves conversation before clearing with special prefix.</summary>
    public void SavePreClearSnapshot(List<ChatMessage> messages)
    {
        if (messages.Count <= 1) return;

        EnsureHistoryDirectory();
        var json = JsonSerializer.Serialize(messages);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var filename = $"{BlueBerryConstants.SnapshotPrefixes.PreClear}{timestamp}.json";
        File.WriteAllText(Path.Combine(HistoryFolder, filename), json);
        Console.WriteLine($"{BlueBerryConstants.Emojis.Save} Conversation saved before clearing to {HistoryFolder}/{filename}");
    }

    /// <summary>Saves quota exceeded snapshot and throws QuotaExceededException.</summary>
    public void SaveQuotaExceededSnapshot(List<ChatMessage> messages, string errorMessage)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n{BlueBerryConstants.Emojis.Alert} DAILY TOKEN QUOTA EXCEEDED {BlueBerryConstants.Emojis.Alert}");
        Console.WriteLine($"Message: {errorMessage}");
        Console.ResetColor();

        EnsureHistoryDirectory();
        var json = JsonSerializer.Serialize(messages);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var filename = Path.Combine(HistoryFolder, $"{BlueBerryConstants.SnapshotPrefixes.QuotaExceeded}{timestamp}.json");
        File.WriteAllText(filename, json);

        Console.WriteLine($"Conversation saved to {filename}");
        throw new QuotaExceededException(errorMessage);
    }

    /// <summary>Displays the loaded conversation in a readable format.</summary>
    private static void DisplayConversation(List<ChatMessage> messages)
    {
        Console.WriteLine("\n📝 LOADED CONVERSATION HISTORY\n");

        for (int i = 0; i < messages.Count; i++)
        {
            var message = messages[i];

            // Skip system message for display (first message)
            if (i == 0 && message.Role == ChatRole.System)
                continue;

            // Display role with appropriate color
            if (message.Role == ChatRole.User)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("👤 USER: ");
            }
            else if (message.Role == ChatRole.Assistant)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write("🤖 ASSISTANT: ");
            }
            else if (message.Role == ChatRole.Tool)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("🔧 TOOL: ");
            }
            Console.ResetColor();

            // Display message content with proper formatting
            var content = message.Text ?? string.Empty;
            if (!string.IsNullOrEmpty(content))
            {
                // Add indentation for readability only for verbose content.
                var lines = content.Split('\n');
                if (lines.Length == 1)
                {
                    // For single line messages, append directly
                    Console.WriteLine(lines[0]);
                }
                else
                {
                    Console.WriteLine();
                    foreach (var line in lines)
                    {
                        Console.WriteLine($"  {line}");
                    }
                }
            }
        }

        Console.WriteLine("\n📝 END OF LOADED CONVERSATION\n");
    }

    private static List<ChatMessage> CreateNewConversation(string systemPrompt) => 
        new() { new ChatMessage(ChatRole.System, systemPrompt) };

    private static void EnsureHistoryDirectory()
    {
        if (!Directory.Exists(HistoryFolder))
            Directory.CreateDirectory(HistoryFolder);
    }
}
