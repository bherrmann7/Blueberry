using System.Text.Json;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace BlueBerry;

/// <summary>Manages MCP server configuration and tool loading.</summary>
public class McpClientManager : IMcpClientManager
{
    private readonly List<IMcpClient> _clients = new();
    private readonly List<McpClientTool> _tools = new();
    private readonly McpConfigurationManager _configManager;

    public McpClientManager()
    {
        _configManager = new McpConfigurationManager();
    }

    public McpClientManager(McpConfigurationManager configManager)
    {
        _configManager = configManager;
    }

    /// <summary>Initializes MCP clients from configuration file.</summary>
    public async Task InitializeAsync(IChatClient samplingClient)
    {
        if (!_configManager.ConfigFileExists)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"No MCP servers configured (aka no {_configManager.ConfigPath}");
            Console.ResetColor();
            return;
        }

        var mcpConfig = _configManager.LoadConfiguration();

        if (mcpConfig?.McpServers == null) return;

        foreach (var serverConfig in mcpConfig.McpServers)
        {
            var client = await McpClientFactory.CreateAsync(
                new StdioClientTransport(new StdioClientTransportOptions
                {
                    Command = serverConfig.Command,
                    Arguments = serverConfig.Arguments,
                    Name = serverConfig.Name
                }),
                new McpClientOptions
                {
                    Capabilities = new ClientCapabilities 
                    { 
                        Sampling = new SamplingCapability 
                        { 
                            SamplingHandler = samplingClient.CreateSamplingHandler() 
                        } 
                    }
                });
            _clients.Add(client);
        }

        foreach (var client in _clients)
            _tools.AddRange(await client.ListToolsAsync());

        Console.WriteLine($"  Tools: {string.Join(", ", _tools)}");
    }

    /// <summary>Gets all available tools.</summary>
    public IReadOnlyList<McpClientTool> Tools => _tools.AsReadOnly();

    /// <summary>Disposes all MCP clients.</summary>
    public void Dispose()
    {
        foreach (var client in _clients)
        {
            if (client is IDisposable disposable)
                disposable.Dispose();
        }
        _clients.Clear();
        _tools.Clear();
    }
}