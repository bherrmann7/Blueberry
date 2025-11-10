using System.Text.Json;

namespace BlueBerry;

/// <summary>
/// Manages MCP configuration loading and provides access to the in-memory model.
/// </summary>
public class McpConfigurationManager
{
    private readonly string _configPath;
    private McpConfig? _cachedConfig;
    private DateTime _lastModified;

    public McpConfigurationManager()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _configPath = Path.Combine(homeDirectory, ".bb", "mcp.json");
    }

    public McpConfigurationManager(string configPath)
    {
        _configPath = configPath;
    }

    /// <summary>
    /// Loads and returns the MCP configuration from the JSON file.
    /// </summary>
    /// <returns>The deserialized MCP configuration, or null if the file doesn't exist or can't be loaded.</returns>
    public McpConfig? LoadConfiguration()
    {
        if (!File.Exists(_configPath))
        {
            return null;
        }

        try
        {
            var fileInfo = new FileInfo(_configPath);
            if (_cachedConfig == null || fileInfo.LastWriteTime > _lastModified)
            {
                var json = File.ReadAllText(_configPath);
                _cachedConfig = JsonSerializer.Deserialize<McpConfig>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                _lastModified = fileInfo.LastWriteTime;
            }

            return _cachedConfig;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error reading MCP configuration: {ex.Message}");
            Console.ResetColor();
            return null;
        }
    }

    /// <summary>
    /// Gets the path to the MCP configuration file.
    /// </summary>
    public string ConfigPath => _configPath;

    /// <summary>
    /// Checks if the MCP configuration file exists.
    /// </summary>
    public bool ConfigFileExists => File.Exists(_configPath);
}