using Microsoft.Extensions.AI;
using Xunit;
using Xunit.Abstractions;
using BluelBerry;

namespace bb.Tests;

[Collection("Integration")]
public class OllamaIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public OllamaIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task SendPhrase_ToOllama_ShouldReceiveResponse()
    {
        // Arrange - Use qwen3:30b model with Ollama
        var options = new AppOptions(
            model: "qwen3:30b",
            endpoint: "http://127.0.0.1:11434/v1",
            key: "not used with ollama"
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
        catch (HttpRequestException ex) when (ex.Message.Contains("Connection refused") || ex.Message.Contains("No connection could be made"))
        {
            // Skip test if Ollama is not running
            _output.WriteLine("Skipping test - Ollama is not running on localhost:11434");
            throw new SkipException("Ollama is not running");
        }
    }

    [Fact]
    public async Task SendSimpleMathQuestion_ToOllama_ShouldCalculateCorrectly()
    {
        // Arrange
        var options = new AppOptions(
            model: "qwen3:30b",
            endpoint: "http://127.0.0.1:11434/v1",
            key: "not used with ollama"
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
        catch (HttpRequestException ex) when (ex.Message.Contains("Connection refused") || ex.Message.Contains("No connection could be made"))
        {
            _output.WriteLine("Skipping test - Ollama is not running on localhost:11434");
            throw new SkipException("Ollama is not running");
        }
    }
}

/// <summary>Custom exception to skip tests when dependencies are not available</summary>
public class SkipException : Exception
{
    public SkipException(string message) : base(message) { }
}