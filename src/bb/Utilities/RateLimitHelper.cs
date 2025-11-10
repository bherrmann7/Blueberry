namespace BlueBerry;

/// <summary>
/// Helper for logging rate limit events during agent execution.
/// Rate limiting is common when making API calls to LLM providers.
/// </summary>
public static class RateLimitHelper
{
    /// <summary>
    /// Logs a rate limit event with retry information.
    /// This helps users understand why the agent is pausing during execution.
    /// </summary>
    public static void LogRateLimit(int attempt, int delaySeconds)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"--- Rate limit encountered. Retrying in {delaySeconds}s... (attempt {attempt})");
        Console.ResetColor();
    }
}
