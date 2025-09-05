using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

public class RateLimitLoggingHandler : DelegatingHandler
{
    private readonly ILogger<RateLimitLoggingHandler> _logger;

    public RateLimitLoggingHandler(ILogger<RateLimitLoggingHandler> logger)
        : base(new HttpClientHandler())
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        // Only log rate limit headers if they're interesting (low remaining)
        var rateLimitHeaders = response.Headers
            .Where(h => h.Key.StartsWith("x-ratelimit", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var showRateLimitInfo = false;
        if (rateLimitHeaders.Any())
        {
            // Check if we're getting low on any limits
            foreach (var header in rateLimitHeaders)
                if (header.Key.Contains("remaining", StringComparison.OrdinalIgnoreCase))
                {
                    var values = string.Join(", ", header.Value);
                    if (int.TryParse(values, out var remaining))
                    {
                        if (header.Key.Contains("requests") && remaining < 20)
                            showRateLimitInfo = true;
                        if (header.Key.Contains("tokens") && remaining < 10000)
                            showRateLimitInfo = true;
                    }
                }

            if (showRateLimitInfo)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("📊 Rate limit warning - getting low:");
                foreach (var header in rateLimitHeaders.Where(h => h.Key.Contains("remaining"))) Console.WriteLine($"   {header.Key}: {string.Join(", ", header.Value)}");
                Console.ResetColor();
            }
        }

        // Check for token quota exceeded in response body
        if (response.Content != null && response.Content.Headers?.ContentType?.MediaType == "application/json")
        {
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!string.IsNullOrEmpty(responseContent))
            {
                try
                {
                    // Check if response contains the specific Cerebras token quota error
                    if (responseContent.Contains("token_quota_exceeded", StringComparison.OrdinalIgnoreCase) ||
                        responseContent.Contains("too many tokens processed", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("\n🚨 CEREBRAS TOKEN QUOTA EXCEEDED 🚨");

                        try
                        {
                            using var jsonDoc = JsonDocument.Parse(responseContent);
                            var prettyJson = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions
                            {
                                WriteIndented = true
                            });
                            Console.WriteLine(prettyJson);
                        }
                        catch
                        {
                            Console.WriteLine(responseContent);
                        }
                        Console.ResetColor();

                        // Log to file but don't spam console
                        _logger.LogError("Cerebras token quota exceeded: {ResponseContent}", responseContent);
                    }
                }
                catch (JsonException)
                {
                    // If it's not valid JSON, check for error strings in plain text
                    if (responseContent.Contains("token_quota_exceeded", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("\n🚨 TOKEN QUOTA EXCEEDED 🚨");
                        Console.WriteLine($"Response: {responseContent}");
                        Console.ResetColor();

                        _logger.LogError("Token quota exceeded (plain text): {ResponseContent}", responseContent);
                    }
                }

                // Create new content with the same data since we consumed the stream
                response.Content = new StringContent(responseContent, Encoding.UTF8, "application/json");
            }
        }

        // Log HTTP error status codes with minimal noise
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⏱️ Rate limited (HTTP 429)");

                if (response.Headers.RetryAfter != null)
                {
                    var retryAfter = response.Headers.RetryAfter.Delta?.TotalSeconds ??
                                     response.Headers.RetryAfter.Date?.Subtract(DateTimeOffset.UtcNow).TotalSeconds ?? 0;

                    if (retryAfter > 3600) // More than 1 hour
                        Console.WriteLine($"   Retry after: {retryAfter / 3600:F1} hours");
                    else if (retryAfter > 60) // More than 1 minute
                        Console.WriteLine($"   Retry after: {retryAfter / 60:F1} minutes");
                    else
                        Console.WriteLine($"   Retry after: {retryAfter:F0} seconds");
                }

                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ HTTP Error: {(int)response.StatusCode} {response.StatusCode}");
                Console.ResetColor();
            }
        }

        return response;
    }
}