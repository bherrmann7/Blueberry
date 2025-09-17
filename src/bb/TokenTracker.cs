using System.Diagnostics.Metrics;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace BluelBerry;

public class TokenUsageInfo
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int CachedTokens { get; set; }
    public int TotalTokens => InputTokens + OutputTokens;
    public decimal Cost { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string ModelName { get; set; } = string.Empty;
    public int ContextLength { get; set; }
    public int MaxContextLength { get; set; }
    public double ContextUtilization => MaxContextLength > 0 ? (double)ContextLength / MaxContextLength : 0;
}

public class SessionSummary
{
    public int TotalRequests { get; set; }
    public int TotalInputTokens { get; set; }
    public int TotalOutputTokens { get; set; }
    public int TotalTokens => TotalInputTokens + TotalOutputTokens;
    public decimal TotalCost { get; set; }
    public DateTime SessionStart { get; set; } = DateTime.UtcNow;
    public TimeSpan SessionDuration => DateTime.UtcNow - SessionStart;
    public double AvgContextUtilization { get; set; }
    public int MaxContextUsed { get; set; }
}

// Keeping ModelPricing for backward-compatibility (unused). Prefer EnhancedTokenHelper.CalculateCost
public static class ModelPricing
{
    // Pricing per million tokens - legacy minimal set. Not used anymore.
    private static readonly Dictionary<string, (decimal input, decimal output)> Prices = new()
    {
        ["gpt-4o"] = (2.50m, 10.00m),
        ["gpt-4o-mini"] = (0.15m, 0.60m),
        ["gpt-4-turbo"] = (10.00m, 30.00m),
        ["gpt-4"] = (30.00m, 60.00m),
        ["gpt-3.5-turbo"] = (0.50m, 1.50m)
    };

    public static (decimal input, decimal output) GetPricing(string modelName)
    {
        if (Prices.TryGetValue(modelName, out var pricing))
            return pricing;
        foreach (var (key, value) in Prices)
            if (modelName.Contains(key, StringComparison.OrdinalIgnoreCase))
                return value;
        return (1.00m, 3.00m);
    }

    public static decimal CalculateCost(string modelName, int inputTokens, int outputTokens)
    {
        var (inputPrice, outputPrice) = GetPricing(modelName);
        return inputTokens * inputPrice / 1_000_000m + outputTokens * outputPrice / 1_000_000m;
    }
}

public class TokenTracker : IDisposable
{
    private readonly Histogram<double> _contextUtilizationHistogram;
    private readonly Counter<decimal> _costCounter;
    private readonly Counter<int> _inputTokensCounter;
    private readonly Meter _meter;
    private readonly Counter<int> _outputTokensCounter;
    private readonly SessionSummary _sessionSummary = new();
    private readonly List<TokenUsageInfo> _usageHistory = new();

    public TokenTracker()
    {
        _meter = new Meter("BluelBerry.Tokens");
        _inputTokensCounter = _meter.CreateCounter<int>("tokens.input", "tokens", "Input tokens consumed");
        _outputTokensCounter = _meter.CreateCounter<int>("tokens.output", "tokens", "Output tokens generated");
        _costCounter = _meter.CreateCounter<decimal>("cost.total", "USD", "Total cost incurred");
        _contextUtilizationHistogram = _meter.CreateHistogram<double>("context.utilization", "percentage", "Context window utilization");
    }

    public void Dispose()
    {
        _meter?.Dispose();
    }

    public TokenUsageInfo TrackUsage(ChatMessage message, string modelName, int contextLength, int maxContextLength)
    {
        // Extract usage information from message metadata or estimate
        var inputTokens = 0;
        var outputTokens = 0;

        // Try to get actual usage from message metadata if available
        if (message.AdditionalProperties?.TryGetValue("usage", out var usageObj) == true)
            if (usageObj is JsonElement usageJson)
            {
                if (usageJson.TryGetProperty("input_tokens", out var inputProp))
                    inputTokens = inputProp.GetInt32();
                if (usageJson.TryGetProperty("output_tokens", out var outputProp))
                    outputTokens = outputProp.GetInt32();
            }

        // If no usage info available, estimate based on text length
        if (inputTokens == 0 && outputTokens == 0) outputTokens = EnhancedTokenHelper.EstimateTokenCount(message.Text ?? "");

        var info = new TokenUsageInfo
        {
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            ModelName = modelName,
            ContextLength = contextLength,
            MaxContextLength = maxContextLength,
            Cost = EnhancedTokenHelper.CalculateCost(modelName, inputTokens, outputTokens)
        };

        _usageHistory.Add(info);
        UpdateSessionSummary(info);
        RecordMetrics(info);

        return info;
    }

    public TokenUsageInfo EstimateUsage(List<ChatMessage> messages, string modelName, int maxContextLength)
    {
        // Rough estimation: ~4 characters per token
        var totalChars = messages.Sum(m => m.Text?.Length ?? 0);
        var estimatedTokens = totalChars / 4;

        return new TokenUsageInfo
        {
            InputTokens = estimatedTokens,
            OutputTokens = 0,
            ModelName = modelName,
            ContextLength = estimatedTokens,
            MaxContextLength = maxContextLength,
            Cost = EnhancedTokenHelper.CalculateCost(modelName, estimatedTokens, 0)
        };
    }

    public TokenUsageInfo? TrackStreamingUsage(IEnumerable<ChatResponseUpdate> updates, string modelName, int contextLength, int maxContextLength)
    {
        var updatesList = updates?.ToList() ?? new List<ChatResponseUpdate>();
        
        // Usage info typically comes in the last chunk of a streaming response
        var lastUpdate = updatesList.LastOrDefault();
        if (lastUpdate == null)
            return null;

        // Find a UsageContent entry safely
        var usage = lastUpdate.Contents?.OfType<UsageContent>().FirstOrDefault();
        if (usage == null || usage.Details == null)
            return null;

        int inputTokens = (int)(usage.Details.InputTokenCount ?? 0);
        int outputTokens = (int)(usage.Details.OutputTokenCount ?? 0);
        int cachedTokens = 0;

        var additional = usage.Details.AdditionalCounts;
        if (additional != null)
        {
            foreach (var kv in additional)
            {
                var key = kv.Key?.Trim();
                if (string.Equals(key, "InputTokenDetails.CachedTokenCount", StringComparison.OrdinalIgnoreCase) ||
                    (key?.Contains("CachedTokenCount", StringComparison.OrdinalIgnoreCase) == true))
                {
                    cachedTokens = (int)kv.Value;
                }
            }
        }

        // Show detailed cost breakdown (Standard tier by default)
        var cost = EnhancedTokenHelper.CalculateCost(modelName, inputTokens, outputTokens, cachedTokens);
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"🔢 Request Token Usage: prompt={inputTokens}, completion={outputTokens}, cached={cachedTokens}, total={inputTokens + outputTokens} | 💰 Cost: ${cost:F6}");
        Console.ResetColor();
        

        var info = new TokenUsageInfo
        {
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            CachedTokens = cachedTokens,
            ModelName = modelName,
            ContextLength = contextLength,
            MaxContextLength = maxContextLength,
            Cost = cost
        };

        _usageHistory.Add(info);
        UpdateSessionSummary(info);
        RecordMetrics(info);
        return info;
    }

    private void UpdateSessionSummary(TokenUsageInfo info)
    {
        _sessionSummary.TotalRequests++;
        _sessionSummary.TotalInputTokens += info.InputTokens;
        _sessionSummary.TotalOutputTokens += info.OutputTokens;
        _sessionSummary.TotalCost += info.Cost;
        _sessionSummary.MaxContextUsed = Math.Max(_sessionSummary.MaxContextUsed, info.ContextLength);
        _sessionSummary.AvgContextUtilization = _usageHistory.Any() ? _usageHistory.Average(u => u.ContextUtilization) : 0;
    }

    private void RecordMetrics(TokenUsageInfo info)
    {
        var modelTag = new KeyValuePair<string, object?>("model", info.ModelName);

        _inputTokensCounter.Add(info.InputTokens, modelTag);
        _outputTokensCounter.Add(info.OutputTokens, modelTag);
        _costCounter.Add(info.Cost, modelTag);
        _contextUtilizationHistogram.Record(info.ContextUtilization * 100, modelTag);
    }

    public SessionSummary GetSessionSummary()
    {
        return _sessionSummary;
    }

    public IReadOnlyList<TokenUsageInfo> GetUsageHistory()
    {
        return _usageHistory.AsReadOnly();
    }

    public void PrintUsageInfo(TokenUsageInfo info)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"💰 Tokens: {info.InputTokens:N0} in + {info.OutputTokens:N0} out = {info.TotalTokens:N0} total");
        Console.WriteLine($"🪙 Cost: ${info.Cost:F4} | Context: {info.ContextLength:N0}/{info.MaxContextLength:N0} ({info.ContextUtilization:P1})");
        Console.ResetColor();
    }

    public void PrintSessionSummary()
    {
        var summary = _sessionSummary;
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("\n📊 Session Summary:");
        Console.WriteLine($"   Requests: {summary.TotalRequests}");
        Console.WriteLine($"   Tokens: {summary.TotalInputTokens:N0} in + {summary.TotalOutputTokens:N0} out = {summary.TotalTokens:N0} total");
        Console.WriteLine($"   Total Cost: ${summary.TotalCost:F4}");
        Console.WriteLine($"   Duration: {summary.SessionDuration:hh\\:mm\\:ss}");
        Console.WriteLine($"   Avg Context Usage: {summary.AvgContextUtilization:P1}");
        Console.WriteLine($"   Max Context Used: {summary.MaxContextUsed:N0}");
        Console.ResetColor();
    }

    public void SaveSessionReport(string filePath)
    {
        var report = new
        {
            Summary = _sessionSummary,
            UsageHistory = _usageHistory,
            GeneratedAt = DateTime.UtcNow
        };

        var json = System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }
}
