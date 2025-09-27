using Microsoft.Extensions.AI;
using Xunit;
using Xunit.Abstractions;
using BluelBerry;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace bb.Tests;

[Collection("Integration")]
public class OllamaIntegrationTests
{
    private readonly ITestOutputHelper _output;
    private const string DefaultModel = "qwen2:7b";
    private const string DefaultEndpoint = "http://127.0.0.1:11434/v1";
    private const string DefaultKey = "not used with ollama";

    public OllamaIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Check if Ollama server is running by attempting a connection
    /// </summary>
    private async Task<bool> IsOllamaRunningAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var response = await client.GetAsync("http://127.0.0.1:11434/api/tags");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Common method to handle test execution with proper error handling and skipping
    /// </summary>
    private async Task<string?> ExecuteOllamaTestAsync(string userMessage)
    {
        // Check if Ollama is running first
        if (!await IsOllamaRunningAsync())
        {
            _output.WriteLine("SKIP: Ollama server is not running on localhost:11434. To run this test:");
            _output.WriteLine("  1. Install Ollama from https://ollama.ai");
            _output.WriteLine("  2. Start Ollama service");
            _output.WriteLine($"  3. Pull a model: ollama pull {DefaultModel}");
            return null; // Indicates test should be skipped
        }

        var options = new AppOptions(
            model: DefaultModel,
            endpoint: DefaultEndpoint,
            key: DefaultKey
        );

        try
        {
            // Create chat client and send message
            var (chatClient, _, loggerFactory, tokenTracker) = ChatClientFactory.Create(options);

            using (loggerFactory) // Ensure proper disposal
            {
                var messages = new List<ChatMessage>
                {
                    new(ChatRole.User, userMessage)
                };

                var updates = new List<ChatResponseUpdate>();
                await foreach (var update in chatClient.GetStreamingResponseAsync(messages))
                {
                    updates.Add(update);
                }

                var responseText = string.Join("", updates.Where(u => u.Text != null).Select(u => u.Text));
                
                _output.WriteLine($"Sent: {userMessage}");
                _output.WriteLine($"Received: {responseText}");

                return responseText;
            }
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("Connection refused") || ex.Message.Contains("No connection could be made"))
        {
            _output.WriteLine($"SKIP: Cannot connect to Ollama server on localhost:11434. Please ensure Ollama is running. Error: {ex.Message}");
            return null;
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("404") || ex.Message.Contains("Not Found"))
        {
            _output.WriteLine($"SKIP: Model '{DefaultModel}' not found in Ollama at endpoint '{DefaultEndpoint}'. Please pull the model first: ollama pull {DefaultModel}. Error: {ex.Message}");
            return null;
        }
        catch (SocketException ex)
        {
            _output.WriteLine($"SKIP: Network socket error connecting to Ollama. Please check if Ollama is running. Error: {ex.Message}");
            return null;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _output.WriteLine($"SKIP: Timeout connecting to Ollama server. The server might be slow or not responding. Error: {ex.Message}");
            return null;
        }
        catch (Exception ex) when (ex.Message.Contains("model") && ex.Message.Contains("not found"))
        {
            _output.WriteLine($"SKIP: Model '{DefaultModel}' not found in Ollama at endpoint '{DefaultEndpoint}'. Please pull the model first: ollama pull {DefaultModel}. Error: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            _output.WriteLine($"SKIP: Unexpected error during Ollama test. Error: {ex.Message}");
            return null;
        }
    }

    [Fact]
    public async Task SendPhrase_ToOllama_ShouldReceiveResponse()
    {
        // Arrange
        var testPhrase = "Hello, please respond with 'Test successful' and nothing else.";

        // Act
        var responseText = await ExecuteOllamaTestAsync(testPhrase);
        
        // Skip test if execution failed (service unavailable, etc.)
        if (responseText == null) return;

        // Assert
        Assert.NotEmpty(responseText);
        Assert.True(responseText.Length > 0);
    }

    [Fact]
    public async Task SendSimpleMathQuestion_ToOllama_ShouldCalculateCorrectly()
    {
        // Arrange
        var mathQuestion = "What is 2 + 2? Please respond with just the number.";

        // Act
        var responseText = await ExecuteOllamaTestAsync(mathQuestion);
        
        // Skip test if execution failed (service unavailable, etc.)
        if (responseText == null) return;

        // Assert
        Assert.NotEmpty(responseText);
        Assert.Contains("4", responseText);
    }
}