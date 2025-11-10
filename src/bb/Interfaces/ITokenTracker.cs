using Microsoft.Extensions.AI;

namespace BlueBerry;

/// <summary>
/// Tracks token usage, costs, and context window utilization.
/// This demonstrates the "token burning" problem - every request sends full history.
///
/// Production agents solve this with:
/// - Conversation summarization
/// - Sliding context windows
/// - Vector databases (RAG)
/// - Semantic caching
/// </summary>
public interface ITokenTracker : IDisposable
{
    /// <summary>
    /// Tracks token usage from a streaming response.
    /// Returns detailed usage information including input/output/cached tokens.
    /// </summary>
    TokenUsageInfo? TrackStreamingUsage(
        IEnumerable<ChatResponseUpdate> updates,
        string modelName,
        int contextLength,
        int maxContextLength);

    /// <summary>
    /// Gets a summary of the entire session's token usage and costs.
    /// </summary>
    SessionSummary GetSessionSummary();

    /// <summary>
    /// Prints a formatted summary of the session to the console.
    /// </summary>
    void PrintSessionSummary();

    /// <summary>
    /// Saves a detailed session report to a JSON file for analysis.
    /// </summary>
    void SaveSessionReport(string filePath);
}
