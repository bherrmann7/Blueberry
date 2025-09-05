using System.Text.Json;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace BluelBerry;

/// <summary>Manages MCP server configuration and tool loading.</summary>
public class McpClientManager
{
    private readonly List<IMcpClient> _clients = new();
    private readonly List<McpClientTool> _tools = new();

    /// <summary>Initializes MCP clients from configuration file.</summary>
    public async Task InitializeAsync(IChatClient samplingClient)
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var mcpConfigPath = Path.Combine(homeDirectory, ".bb", "mcp.json");

        if (!File.Exists(mcpConfigPath))
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"No MCP servers configured (aka no {mcpConfigPath}");
            Console.ResetColor();
            return;
        }

        var mcpConfigJson = File.ReadAllText(mcpConfigPath);
        var mcpConfig = JsonSerializer.Deserialize<McpConfig>(mcpConfigJson, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

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

        Console.WriteLine($"Tools available: {string.Join(", ", _tools)}");
        Console.WriteLine();
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