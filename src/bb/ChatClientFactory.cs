using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace BlueBerry;

/// <summary>
/// Factory for creating configured chat clients with logging and rate limiting.
/// </summary>
public class ChatClientFactory
{
    /// <summary>
    /// Creates a chat client with logging, rate limiting, and function invocation support.
    /// </summary>
    public static (IChatClient chatClient, IChatClient samplingClient, ILoggerFactory loggerFactory, TokenTracker tokenTracker) Create(AppOptions options)
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        // Initialize token tracker
        var tokenTracker = new TokenTracker();

        var rateLimitLogger = loggerFactory.CreateLogger<RateLimitLoggingHandler>();
        var rateLimitHandler = new RateLimitLoggingHandler(rateLimitLogger, options.enableHttpLogging);

        var openAIClient = new OpenAIClient(
            new ApiKeyCredential(options.key),
            new OpenAIClientOptions 
            { 
                Endpoint = new Uri(options.endpoint), 
                Transport = new HttpClientPipelineTransport(new HttpClient(rateLimitHandler)) 
            }).GetChatClient(options.model);

        // Create sampling client
        var samplingClient = openAIClient.AsIChatClient();

        // Create main chat client with function invocation
        var chatClient = openAIClient.AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();

        return (chatClient, samplingClient, loggerFactory, tokenTracker);
    }
}