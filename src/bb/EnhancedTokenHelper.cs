using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.AI;

namespace BlueBerry;

public enum PricingTier
{
    Batch,
    Flex,
    Standard,
    Priority
}

public record ModelPrice(decimal Input, decimal Output, decimal? CachedInput = null);

public static class EnhancedTokenHelper
{
    // Known max token windows for common models
    private static readonly Dictionary<string, int> ModelMaxTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        // OpenAI models (common defaults)
        ["gpt-5"] = 128_000,
        ["gpt-5-mini"] = 128_000,
        ["gpt-5-nano"] = 128_000,
        ["gpt-4.1"] = 128_000,
        ["gpt-4.1-mini"] = 128_000,
        ["gpt-4.1-nano"] = 128_000,
        ["gpt-4o"] = 128_000,
        ["gpt-4o-mini"] = 128_000,
        ["gpt-4o-2024-05-13"] = 128_000,
        ["gpt-4-turbo"] = 128_000,
        ["gpt-4-1106-preview"] = 128_000,
        ["gpt-4"] = 8_192,
        ["gpt-3.5-turbo"] = 16_385,

        // O-series
        ["o1"] = 200_000,
        ["o1-pro"] = 200_000,
        ["o1-mini"] = 200_000,
        ["o3"] = 200_000,
        ["o3-pro"] = 200_000,
        ["o3-mini"] = 200_000,
        ["o3-deep-research"] = 200_000,
        ["o4-mini"] = 200_000,
        ["o4-mini-deep-research"] = 200_000,

        // Realtime/audio (text context for pricing purposes only)
        ["gpt-realtime"] = 128_000,
        ["gpt-4o-realtime-preview"] = 128_000,
        ["gpt-4o-mini-realtime-preview"] = 128_000,
        ["gpt-audio"] = 128_000,
        ["gpt-4o-audio-preview"] = 128_000,
        ["gpt-4o-mini-audio-preview"] = 128_000,

        // Cerebras models
        ["llama3.1-8b"] = 128_000,
        ["llama3.1-70b"] = 128_000,
        ["llama-3.3-70b"] = 128_000

        // Add more models as needed
    };

    // Pricing per 1M tokens for text tokens
    private static readonly Dictionary<PricingTier, Dictionary<string, ModelPrice>> TextPrices
        = new()
        {
            [PricingTier.Batch] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["gpt-5"] = new(0.625m, 5.00m, 0.0625m),
                ["gpt-5-mini"] = new(0.125m, 1.00m, 0.0125m),
                ["gpt-5-nano"] = new(0.025m, 0.20m, 0.0025m),
                ["gpt-4.1"] = new(1.00m, 4.00m, null),
                ["gpt-4.1-mini"] = new(0.20m, 0.80m, null),
                ["gpt-4.1-nano"] = new(0.05m, 0.20m, null),
                ["gpt-4o"] = new(1.25m, 5.00m, null),
                ["gpt-4o-2024-05-13"] = new(2.50m, 7.50m, null),
                ["gpt-4o-mini"] = new(0.075m, 0.30m, null),
                ["o1"] = new(7.50m, 30.00m, null),
                ["o1-pro"] = new(75.00m, 300.00m, null),
                ["o3-pro"] = new(10.00m, 40.00m, null),
                ["o3"] = new(1.00m, 4.00m, null),
                ["o3-deep-research"] = new(5.00m, 20.00m, null),
                ["o4-mini"] = new(0.55m, 2.20m, null),
                ["o4-mini-deep-research"] = new(1.00m, 4.00m, null),
                ["o3-mini"] = new(0.55m, 2.20m, null),
                ["o1-mini"] = new(0.55m, 2.20m, null),
                ["computer-use-preview"] = new(1.50m, 6.00m, null)
            },
            [PricingTier.Flex] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["gpt-5"] = new(0.625m, 5.00m, 0.0625m),
                ["gpt-5-mini"] = new(0.125m, 1.00m, 0.0125m),
                ["gpt-5-nano"] = new(0.025m, 0.20m, 0.0025m),
                ["o3"] = new(1.00m, 4.00m, 0.25m),
                ["o4-mini"] = new(0.55m, 2.20m, 0.138m)
            },
            [PricingTier.Standard] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["gpt-5"] = new(1.25m, 10.00m, 0.125m),
                ["gpt-5-mini"] = new(0.25m, 2.00m, 0.025m),
                ["gpt-5-nano"] = new(0.05m, 0.40m, 0.005m),
                ["gpt-5-chat-latest"] = new(1.25m, 10.00m, 0.125m),
                ["gpt-4.1"] = new(2.00m, 8.00m, 0.50m),
                ["gpt-4.1-mini"] = new(0.40m, 1.60m, 0.10m),
                ["gpt-4.1-nano"] = new(0.10m, 0.40m, 0.025m),
                ["gpt-4o"] = new(2.50m, 10.00m, 1.25m),
                ["gpt-4o-2024-05-13"] = new(5.00m, 15.00m, null),
                ["gpt-4o-mini"] = new(0.15m, 0.60m, 0.075m),
                ["gpt-realtime"] = new(4.00m, 16.00m, 0.40m),
                ["gpt-4o-realtime-preview"] = new(5.00m, 20.00m, 2.50m),
                ["gpt-4o-mini-realtime-preview"] = new(0.60m, 2.40m, 0.30m),
                ["gpt-audio"] = new(2.50m, 10.00m, null),
                ["gpt-4o-audio-preview"] = new(2.50m, 10.00m, null),
                ["gpt-4o-mini-audio-preview"] = new(0.15m, 0.60m, null),
                ["o1"] = new(15.00m, 60.00m, 7.50m),
                ["o1-pro"] = new(150.00m, 600.00m, null),
                ["o3-pro"] = new(20.00m, 80.00m, null),
                ["o3"] = new(2.00m, 8.00m, 0.50m),
                ["o3-deep-research"] = new(10.00m, 40.00m, 2.50m),
                ["o4-mini"] = new(1.10m, 4.40m, 0.275m),
                ["o4-mini-deep-research"] = new(2.00m, 8.00m, 0.50m),
                ["o3-mini"] = new(1.10m, 4.40m, 0.55m),
                ["o1-mini"] = new(1.10m, 4.40m, 0.55m),
                ["codex-mini-latest"] = new(1.50m, 6.00m, 0.375m),
                ["gpt-4o-mini-search-preview"] = new(0.15m, 0.60m, null),
                ["gpt-4o-search-preview"] = new(2.50m, 10.00m, null),
                ["computer-use-preview"] = new(3.00m, 12.00m, null),
                ["gpt-image-1"] = new(5.00m, 0.00m, 1.25m), // Output not applicable for text tokens, set 0
                // Legacy models (Standard tier)
                ["chatgpt-4o-latest"] = new(5.00m, 15.00m, null),
                ["gpt-4-turbo-2024-04-09"] = new(10.00m, 30.00m, null),
                ["gpt-4-0125-preview"] = new(10.00m, 30.00m, null),
                ["gpt-4-1106-preview"] = new(10.00m, 30.00m, null),
                ["gpt-4-1106-vision-preview"] = new(10.00m, 30.00m, null),
                ["gpt-4-0613"] = new(30.00m, 60.00m, null),
                ["gpt-4-0314"] = new(30.00m, 60.00m, null),
                ["gpt-4-32k"] = new(60.00m, 120.00m, null),
                ["gpt-3.5-turbo"] = new(0.50m, 1.50m, null),
                ["gpt-3.5-turbo-0125"] = new(0.50m, 1.50m, null),
                ["gpt-3.5-turbo-1106"] = new(1.00m, 2.00m, null),
                ["gpt-3.5-turbo-0613"] = new(1.50m, 2.00m, null),
                ["gpt-3.5-0301"] = new(1.50m, 2.00m, null),
                ["gpt-3.5-turbo-instruct"] = new(1.50m, 2.00m, null),
                ["gpt-3.5-turbo-16k-0613"] = new(3.00m, 4.00m, null),
                ["davinci-002"] = new(2.00m, 2.00m, null),
                ["babbage-002"] = new(0.40m, 0.40m, null)
            },
            [PricingTier.Priority] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["gpt-5"] = new(2.50m, 20.00m, 0.25m),
                ["gpt-5-mini"] = new(0.45m, 3.60m, 0.045m),
                ["gpt-4.1"] = new(3.50m, 14.00m, 0.875m),
                ["gpt-4.1-mini"] = new(0.70m, 2.80m, 0.175m),
                ["gpt-4.1-nano"] = new(0.20m, 0.80m, 0.05m),
                ["gpt-4o"] = new(4.25m, 17.00m, 2.125m),
                ["gpt-4o-2024-05-13"] = new(8.75m, 26.25m, null),
                ["gpt-4o-mini"] = new(0.25m, 1.00m, 0.125m),
                ["o3"] = new(3.50m, 14.00m, 0.875m),
                ["o4-mini"] = new(2.00m, 8.00m, 0.50m)
            }
        };

    public static ModelPrice GetPricing(string modelName, PricingTier tier = PricingTier.Standard)
    {
        var tierPrices = TextPrices.TryGetValue(tier, out var dict)
            ? dict
            : TextPrices[PricingTier.Standard];

        // Exact match
        if (tierPrices.TryGetValue(modelName, out var price))
            return price;

        // Family/contains match
        foreach (var kvp in tierPrices)
        {
            if (modelName.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }

        // Fallback: try Standard tier if not already
        if (tier != PricingTier.Standard)
        {
            return GetPricing(modelName, PricingTier.Standard);
        }

        // Default pricing if unknown: conservative default
        return new ModelPrice(1.00m, 3.00m, null);
    }

    public static decimal CalculateCost(
        string modelName,
        int inputTokens,
        int outputTokens,
        int cachedInputTokens = 0,
        PricingTier tier = PricingTier.Standard)
    {
        var p = GetPricing(modelName, tier);
        var billableFreshInput = Math.Max(0, inputTokens - Math.Max(0, cachedInputTokens));
        var cached = Math.Max(0, cachedInputTokens);

        var inputCost = (billableFreshInput * p.Input) / 1_000_000m;
        var cachedCost = p.CachedInput.HasValue
            ? (cached * p.CachedInput.Value) / 1_000_000m
            : (cached * p.Input) / 1_000_000m; // if no cached price, bill at regular input
        var outputCost = (outputTokens * p.Output) / 1_000_000m;

        return inputCost + cachedCost + outputCost;
    }

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