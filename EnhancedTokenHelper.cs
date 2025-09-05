using Microsoft.Extensions.AI;

namespace BluelBerry;

public static class EnhancedTokenHelper
{
    // Known max token windows for common models
    private static readonly Dictionary<string, int> ModelMaxTokens = new()
    {
        // OpenAI models
        ["gpt-4o"] = 128_000,
        ["gpt-4o-mini"] = 128_000,
        ["gpt-4-turbo"] = 128_000,
        ["gpt-4-1106-preview"] = 128_000,
        ["gpt-4"] = 8_192,
        ["gpt-3.5-turbo"] = 16_385,

        // Cerebras models
        ["llama3.1-8b"] = 128_000,
        ["llama3.1-70b"] = 128_000,
        ["llama-3.3-70b"] = 128_000

        // Add more models as needed
    };

    public static int GetMaxTokens(string modelName)
    {
        // Try exact match first
        if (ModelMaxTokens.TryGetValue(modelName, out var max))
            return max;

        // Try partial matches for model families
        foreach (var (key, value) in ModelMaxTokens)
            if (modelName.Contains(key, StringComparison.OrdinalIgnoreCase))
                return value;

        // Default fallback
        return 32_000;
    }

    public static int EstimateTokenCount(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        // Rough estimation: ~4 characters per token for English text
        // This is an approximation - actual tokenization varies by model
        return text.Length / 4;
    }

    public static int EstimateTokenCount(IEnumerable<ChatMessage> messages)
    {
        return messages.Sum(m => EstimateTokenCount(m.Text ?? ""));
    }

    public static bool IsContextNearLimit(int currentTokens, int maxTokens, double warningThreshold = 0.8)
    {
        return (double)currentTokens / maxTokens >= warningThreshold;
    }

    public static string FormatTokenCount(int tokens)
    {
        return tokens switch
        {
            >= 1_000_000 => $"{tokens / 1_000_000.0:F1}M",
            >= 1_000 => $"{tokens / 1_000.0:F1}K",
            _ => tokens.ToString()
        };
    }

    public static void PrintContextWarning(int currentTokens, int maxTokens)
    {
        var utilization = (double)currentTokens / maxTokens;

        if (utilization >= 0.9)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"⚠️  Context nearly full: {FormatTokenCount(currentTokens)}/{FormatTokenCount(maxTokens)} ({utilization:P1})");
        }
        else if (utilization >= 0.7)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"⚡ Context getting full: {FormatTokenCount(currentTokens)}/{FormatTokenCount(maxTokens)} ({utilization:P1})");
        }

        Console.ResetColor();
    }
}