using System.Text.Json;

namespace BluelBerry;

public class McpConfigLoader
{
    private readonly McpConfigurationManager _configManager;

    public McpConfigLoader()
    {
        _configManager = new McpConfigurationManager();
    }

    public McpConfigLoader(McpConfigurationManager configManager)
    {
        _configManager = configManager;
    }

    public static void DisplayMcpServers()
    {
        var loader = new McpConfigLoader();
        loader.DisplayMcpServersImpl();
    }

    private void DisplayMcpServersImpl()
    {
        Console.WriteLine($"MCP configuration file path: {_configManager.ConfigPath}");
        Console.WriteLine(new string('=', 60));

        if (!_configManager.ConfigFileExists)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("No MCP configuration file found.");
            Console.ResetColor();
            return;
        }

        try
        {
            var mcpConfig = _configManager.LoadConfiguration();

            if (mcpConfig?.McpServers == null || mcpConfig.McpServers.Count == 0)
            {
                Console.WriteLine("No MCP servers configured.");
                return;
            }

            Console.WriteLine($"Found {mcpConfig.McpServers.Count} MCP server(s):");
            Console.WriteLine(new string('=', 50));
            
            foreach (var server in mcpConfig.McpServers)
            {
                Console.WriteLine($"Name: {server.Name}");
                Console.WriteLine($"  Command: {server.Command}");
                Console.WriteLine($"  Arguments: [{string.Join(", ", server.Arguments)}]");
                Console.WriteLine(new string('-', 30));
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error reading MCP configuration: {ex.Message}");
            Console.ResetColor();
        }
    }
}