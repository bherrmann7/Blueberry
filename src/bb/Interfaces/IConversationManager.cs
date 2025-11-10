using Microsoft.Extensions.AI;

namespace BlueBerry;

/// <summary>
/// Manages conversation history persistence and loading.
/// This is a key component of the agent architecture - it provides memory across sessions.
/// </summary>
public interface IConversationManager
{
    /// <summary>
    /// Loads the most recent conversation from disk, allowing the agent to resume where it left off.
    /// </summary>
    List<ChatMessage> LoadLatestConversation(string systemPrompt);

    /// <summary>
    /// Saves the current conversation state after each turn.
    /// This enables conversation resumption and debugging.
    /// </summary>
    void SaveConversationSnapshot(List<ChatMessage> messages);

    /// <summary>
    /// Saves conversation before clearing, allowing recovery if needed.
    /// </summary>
    void SavePreClearSnapshot(List<ChatMessage> messages);

    /// <summary>
    /// Saves conversation when quota is exceeded and terminates the session.
    /// </summary>
    void SaveQuotaExceededSnapshot(List<ChatMessage> messages, string errorMessage);
}
