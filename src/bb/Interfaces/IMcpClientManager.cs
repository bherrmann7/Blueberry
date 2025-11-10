using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace BlueBerry;

/// <summary>
/// Manages Model Context Protocol (MCP) server connections and tool discovery.
/// This is how the agent discovers and accesses external tools - the "Act" in ReAct.
///
/// MCP servers provide tools like file system access, web search, database queries, etc.
/// The agent loop will pass these tools to the LLM, which can choose to call them.
/// </summary>
public interface IMcpClientManager : IDisposable
{
    /// <summary>
    /// Initializes all configured MCP servers and discovers their available tools.
    /// This happens at startup, before the agent loop begins.
    /// </summary>
    Task InitializeAsync(IChatClient samplingClient);

    /// <summary>
    /// Gets all discovered tools from all MCP servers.
    /// These tools are passed to the LLM so it knows what actions it can take.
    /// </summary>
    IReadOnlyList<McpClientTool> Tools { get; }
}
