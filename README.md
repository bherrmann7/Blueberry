# Blue Berry (bb)

A .NET 9 console application that connects LLMs to local tools via the Model Context Protocol (MCP).
Think of it as a bridge between your favorite LLM and your development environment.

## What it does

- **Tool Integration**: Connects any OpenAI-compatible LLM to MCP servers (shell, file system, databases, etc.)
- **Cost Tracking**: Real-time token usage and spending analytics
- **Context Monitoring**: Prevents hitting model context limits
- **Interactive REPL**: Chat with the LLM while it executes tools on your behalf

## Quick Start

```bash
# Build
dotnet build

# Run with Cerebras (free tier available)
dotnet run --model qwen-3-coder-480b --endpoint https://api.cerebras.ai/v1 --key $CEREBRAS_API_KEY

# Or with OpenAI
dotnet run --model gpt-4o --endpoint https://api.openai.com/v1 --key $OPENAI_API_KEY
```

## MCP Server Configuration

Create a `mcp.json` file to specify which tools the LLM can access:

**Location:**
- macOS/Linux: `~/.bb/mcp.json` 
- Windows: `C:\Users\{username}\.bb\mcp.json`

**Example:**
```json
{
  "mcp_servers": [
    {
      "name": "Shell",
      "command": "/path/to/shell-mcp-server",
      "arguments": []
    },
    {
      "name": "Files", 
      "command": "/path/to/file-mcp-server",
      "arguments": []
    }
  ]
}
```

## Architecture

1. **LLM Client** → Connects to OpenAI/Cerebras/etc
2. **MCP Manager** → Launches and manages configured tool servers  
3. **Function Calling** → LLM requests tool execution via structured calls
4. **REPL Loop** → Interactive chat with live tool integration
5. **Analytics** → Token tracking and conversation persistence

## Use Cases

- **Code Assistant**: LLM that can read/write files, run builds, execute tests
- **System Admin**: Shell access for deployment, monitoring, troubleshooting
- **Data Analysis**: Database queries, file processing, report generation
- **Development Workflow**: Git operations, package management, environment setup

## Free LLM Access

Sign up at [Cerebras](https://cloud.cerebras.ai?referral_code=y3wvtcmy) for free tokens to get started.

---

Based on the [Microsoft MCP SDK](https://github.com/modelcontextprotocol/csharp-sdk) • MIT License • *Have Fun! 🫐*