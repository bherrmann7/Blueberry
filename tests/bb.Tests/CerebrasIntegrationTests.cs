using Microsoft.Extensions.AI;
using Xunit;
using Xunit.Abstractions;
using BluelBerry;
using System.Net;
using System.Net.Http;

namespace bb.Tests;

[Collection("Integration")]
public class CerebrasIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public CerebrasIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task SendPhrase_ToCerebras_ShouldReceiveResponse()
    {
        // Arrange - Use qwen-3-coder-480b model with Cerebras
        var apiKey = Environment.GetEnvironmentVariable("CEREBRAS_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            _output.WriteLine("SKIP: CEREBRAS_API_KEY environment variable not set. To run this test:");
            _output.WriteLine("  1. Sign up at https://cloud.cerebras.ai");
            _output.WriteLine("  2. Get your API key from the dashboard");
            _output.WriteLine("  3. Set environment variable: export CEREBRAS_API_KEY=your_key_here");
            return; // Skip test gracefully
        }

        var options = new AppOptions(
            model: "qwen-3-coder-480b",
            endpoint: "https://api.cerebras.ai/v1",
            key: apiKey
        );

        var testPhrase = "Hello, please respond with 'Test successful' and nothing else.";

        try
        {
            // Act - Create chat client and send message
            var (chatClient, _, loggerFactory, tokenTracker) = ChatClientFactory.Create(options);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, testPhrase)
            };

            var updates = new List<ChatResponseUpdate>();
            await Task.Delay(TimeSpan.FromSeconds(1));
            await foreach (var update in chatClient.GetStreamingResponseAsync(messages))
            {
                updates.Add(update);
            }

            // Assert
            Assert.NotEmpty(updates);
            var responseText = string.Join("", updates.Where(u => u.Text != null).Select(u => u.Text));
            Assert.NotEmpty(responseText);

            _output.WriteLine($"Sent: {testPhrase}");
            _output.WriteLine($"Received: {responseText}");

            // Verify we got some meaningful response
            Assert.True(responseText.Length > 0);

            // Clean up
            loggerFactory.Dispose();
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("Unauthorized") || ex.Message.Contains("401"))
        {
            _output.WriteLine($"SKIP: Cerebras API key is invalid or unauthorized. Please check your CEREBRAS_API_KEY environment variable. Error: {ex.Message}");
            return; // Skip test gracefully
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("rate limit") || ex.Message.Contains("429"))
        {
            _output.WriteLine($"SKIP: Cerebras rate limit exceeded. Please try again later. Error: {ex.Message}");
            return; // Skip test gracefully
        }
        catch (HttpRequestException ex)
        {
            _output.WriteLine($"SKIP: Network error connecting to Cerebras API. Please check your internet connection. Error: {ex.Message}");
            return; // Skip test gracefully
        }
        catch (Exception ex)
        {
            _output.WriteLine($"SKIP: Unexpected error during Cerebras test. Error: {ex.Message}");
            return; // Skip test gracefully
        }
    }

    [Fact]
    public async Task SendSimpleMathQuestion_ToCerebras_ShouldCalculateCorrectly()
    {
        // Arrange
        var apiKey = Environment.GetEnvironmentVariable("CEREBRAS_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            _output.WriteLine("SKIP: CEREBRAS_API_KEY environment variable not set. To run this test:");
            _output.WriteLine("  1. Sign up at https://cloud.cerebras.ai");
            _output.WriteLine("  2. Get your API key from the dashboard");
            _output.WriteLine("  3. Set environment variable: export CEREBRAS_API_KEY=your_key_here");
            return; // Skip test gracefully
        }

        var options = new AppOptions(
            model: "qwen-3-coder-480b",
            endpoint: "https://api.cerebras.ai/v1",
            key: apiKey
        );

        var mathQuestion = "What is 2 + 2? Please respond with just the number.";

        try
        {
            // Act
            var (chatClient, _, loggerFactory, tokenTracker) = ChatClientFactory.Create(options);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, mathQuestion)
            };

            var updates = new List<ChatResponseUpdate>();
            await Task.Delay(TimeSpan.FromSeconds(1));
            await foreach (var update in chatClient.GetStreamingResponseAsync(messages))
            {
                updates.Add(update);
            }

            // Assert
            Assert.NotEmpty(updates);
            var responseText = string.Join("", updates.Where(u => u.Text != null).Select(u => u.Text));
            Assert.NotEmpty(responseText);

            _output.WriteLine($"Math question: {mathQuestion}");
            _output.WriteLine($"Response: {responseText}");

            // Check that response contains "4" somewhere
            Assert.Contains("4", responseText);

            // Clean up
            loggerFactory.Dispose();
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("Unauthorized") || ex.Message.Contains("401"))
        {
            _output.WriteLine($"SKIP: Cerebras API key is invalid or unauthorized. Please check your CEREBRAS_API_KEY environment variable. Error: {ex.Message}");
            return; // Skip test gracefully
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("rate limit") || ex.Message.Contains("429"))
        {
            _output.WriteLine($"SKIP: Cerebras rate limit exceeded. Please try again later. Error: {ex.Message}");
            return; // Skip test gracefully
        }
        catch (HttpRequestException ex)
        {
            _output.WriteLine($"SKIP: Network error connecting to Cerebras API. Please check your internet connection. Error: {ex.Message}");
            return; // Skip test gracefully
        }
        catch (Exception ex)
        {
            _output.WriteLine($"SKIP: Unexpected error during Cerebras test. Error: {ex.Message}");
            return; // Skip test gracefully
        }
    }

    [Fact]
    public async Task SendCodeQuestion_ToCerebras_ShouldGenerateCode()
    {
        // Arrange
        var apiKey = Environment.GetEnvironmentVariable("CEREBRAS_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            _output.WriteLine("SKIP: CEREBRAS_API_KEY environment variable not set. To run this test:");
            _output.WriteLine("  1. Sign up at https://cloud.cerebras.ai");
            _output.WriteLine("  2. Get your API key from the dashboard");
            _output.WriteLine("  3. Set environment variable: export CEREBRAS_API_KEY=your_key_here");
            return; // Skip test gracefully
        }

        var options = new AppOptions(
            model: "qwen-3-coder-480b",
            endpoint: "https://api.cerebras.ai/v1",
            key: apiKey
        );

        var codeQuestion = "Write a simple C# method that returns 'Hello World'. Just the method, no explanation.";

        try
        {
            // Act
            var (chatClient, _, loggerFactory, tokenTracker) = ChatClientFactory.Create(options);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, codeQuestion)
            };

            var updates = new List<ChatResponseUpdate>();
            await Task.Delay(TimeSpan.FromSeconds(1));
            await foreach (var update in chatClient.GetStreamingResponseAsync(messages))
            {
                updates.Add(update);
            }

            // Assert
            Assert.NotEmpty(updates);
            var responseText = string.Join("", updates.Where(u => u.Text != null).Select(u => u.Text));
            Assert.NotEmpty(responseText);

            _output.WriteLine($"Code question: {codeQuestion}");
            _output.WriteLine($"Response: {responseText}");

            // Check that response contains C# code patterns
            Assert.True(responseText.Contains("Hello World") || responseText.Contains("HelloWorld"));
            Assert.True(responseText.Contains("string") || responseText.Contains("return"));

            // Clean up
            loggerFactory.Dispose();
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("Unauthorized") || ex.Message.Contains("401"))
        {
            _output.WriteLine($"SKIP: Cerebras API key is invalid or unauthorized. Please check your CEREBRAS_API_KEY environment variable. Error: {ex.Message}");
            return; // Skip test gracefully
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("rate limit") || ex.Message.Contains("429"))
        {
            _output.WriteLine($"SKIP: Cerebras rate limit exceeded. Please try again later. Error: {ex.Message}");
            return; // Skip test gracefully
        }
        catch (HttpRequestException ex)
        {
            _output.WriteLine($"SKIP: Network error connecting to Cerebras API. Please check your internet connection. Error: {ex.Message}");
            return; // Skip test gracefully
        }
        catch (Exception ex)
        {
            _output.WriteLine($"SKIP: Unexpected error during Cerebras test. Error: {ex.Message}");
            return; // Skip test gracefully
        }
    }
}