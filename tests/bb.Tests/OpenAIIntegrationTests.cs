using Microsoft.Extensions.AI;
using Xunit;
using Xunit.Abstractions;
using BluelBerry;

namespace bb.Tests;

[Collection("Integration")]
public class OpenAIIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public OpenAIIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task SendPhrase_ToOpenAI_ShouldReceiveResponse()
    {
        // Arrange - Use gpt-4o model with OpenAI
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new SkipException("OPENAI_API_KEY environment variable not set");
        }

        var options = new AppOptions(
            model: "gpt-5-mini",
            endpoint: "https://api.openai.com/v1",
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
            // Skip test if API key is invalid
            _output.WriteLine("Skipping test - Invalid OpenAI API key");
            throw new SkipException("Invalid OpenAI API key");
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("rate limit") || ex.Message.Contains("429"))
        {
            // Skip test if rate limited
            _output.WriteLine("Skipping test - OpenAI rate limit exceeded");
            throw new SkipException("OpenAI rate limit exceeded");
        }
    }

    [Fact]
    public async Task SendSimpleMathQuestion_ToOpenAI_ShouldCalculateCorrectly()
    {
        // Arrange
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new SkipException("OPENAI_API_KEY environment variable not set");
        }

        var options = new AppOptions(
            model: "gpt-5-mini",
            endpoint: "https://api.openai.com/v1",
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
            _output.WriteLine("Skipping test - Invalid OpenAI API key");
            throw new SkipException("Invalid OpenAI API key");
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("rate limit") || ex.Message.Contains("429"))
        {
            _output.WriteLine("Skipping test - OpenAI rate limit exceeded");
            throw new SkipException("OpenAI rate limit exceeded");
        }
    }

    [Fact]
    public async Task SendCodeQuestion_ToOpenAI_ShouldGenerateCode()
    {
        // Arrange
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new SkipException("OPENAI_API_KEY environment variable not set");
        }

        var options = new AppOptions(
            model: "gpt-5-mini",
            endpoint: "https://api.openai.com/v1",
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
            await foreach (var update in chatClient.GetStreamingResponseAsync(messages))
            {
                updates.Add(update);
            }

            // Assert
            Assert.NotEmpty(updates);
            var responseText = string.Join("", updates.Where(u => u.Text != null).Select(u => u.Text));
            Assert.NotEmpty(responseText);

            _output.WriteLine($"Response: {responseText}");

            // Check that response contains C# code patterns
            Assert.True(responseText.Contains("Hello World") || responseText.Contains("HelloWorld"));
            Assert.True(responseText.Contains("string") || responseText.Contains("return"));

            // Clean up
            loggerFactory.Dispose();
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("Unauthorized") || ex.Message.Contains("401"))
        {
            _output.WriteLine("Skipping test - Invalid OpenAI API key");
            throw new SkipException("Invalid OpenAI API key");
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("rate limit") || ex.Message.Contains("429"))
        {
            _output.WriteLine("Skipping test - OpenAI rate limit exceeded");
            throw new SkipException("OpenAI rate limit exceeded");
        }
    }
}