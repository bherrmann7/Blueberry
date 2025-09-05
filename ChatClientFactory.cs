using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace BluelBerry;

/// <summary>Factory for creating configured chat clients with telemetry and rate limiting.</summary>
public class ChatClientFactory
{
    /// <summary>Creates a chat client with telemetry, rate limiting, and function invocation support.</summary>
    public static (IChatClient chatClient, IChatClient samplingClient, ILoggerFactory loggerFactory, TokenTracker tokenTracker) Create(AppOptions options)
    {
        // Enhanced telemetry setup
        var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource("*")
            .AddSource("BluelBerry.*")
            .AddOtlpExporter()
            .Build();

        var metricsProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter("*")
            .AddMeter("BluelBerry.*")
            .AddOtlpExporter()
            .Build();

        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddOpenTelemetry(opt => opt.AddOtlpExporter()).AddConsole());

        // Initialize token tracker
        var tokenTracker = new TokenTracker();

        var rateLimitLogger = loggerFactory.CreateLogger<RateLimitLoggingHandler>();
        var rateLimitHandler = new RateLimitLoggingHandler(rateLimitLogger);

        var openAIClient = new OpenAIClient(
            new ApiKeyCredential(options.key),
            new OpenAIClientOptions 
            { 
                Endpoint = new Uri(options.endpoint), 
                Transport = new HttpClientPipelineTransport(new HttpClient(rateLimitHandler)) 
            }).GetChatClient(options.model);

        // Create sampling client with telemetry
        var samplingClient = openAIClient.AsIChatClient()
            .AsBuilder()
            .UseOpenTelemetry(loggerFactory, configure: o => o.EnableSensitiveData = true)
            .Build();

        // Create main chat client with function invocation
        var chatClient = openAIClient.AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();

        return (chatClient, samplingClient, loggerFactory, tokenTracker);
    }
}