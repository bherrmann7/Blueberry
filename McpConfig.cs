using System.Text.Json.Serialization;

public class McpServerConfig
{
    public string Name { get; set; } = "";
    public string Command { get; set; } = "";
    public List<string> Arguments { get; set; } = new();
}


public class McpConfig
{
    [JsonPropertyName("mcp_servers")] public List<McpServerConfig> McpServers { get; set; } = new();
}