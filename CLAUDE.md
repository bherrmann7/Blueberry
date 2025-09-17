# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Blue Berry (bb) is a .NET 9 console application that bridges LLMs with local development tools via the Model Context Protocol (MCP). It serves as an interactive agent that can execute tools on behalf of LLMs during conversations.

## Build and Run Commands

```bash
# Build the project
dotnet build

# Run locally with default settings (Ollama)
dotnet run --project src/bb

# Run with specific LLM provider
dotnet run --project src/bb -- --model gpt-4o --endpoint https://api.openai.com/v1 --key YOUR_API_KEY

# Run with Cerebras (free tier available)
dotnet run --project src/bb -- --model qwen-3-coder-480b --endpoint https://api.cerebras.ai/v1 --key YOUR_API_KEY

# Run tests
dotnet test

# Build release versions
dotnet publish src/bb -c Release -r win-x64 --self-contained
dotnet publish src/bb -c Release -r osx-x64 --self-contained
dotnet publish src/bb -c Release -r linux-x64 --self-contained
```

## Architecture Overview

The application follows a layered architecture with distinct responsibilities:

### Core Components

- **Program.cs**: Entry point, command-line parsing, and service initialization
- **ChatSession.cs**: Main REPL loop managing streaming conversations and tool execution
- **McpClientManager.cs**: Manages lifecycle of MCP server connections and tool discovery
- **TokenTracker.cs**: Real-time cost tracking, usage analytics, and context monitoring
- **ConversationManager.cs**: Persistent conversation history and snapshot management

### Key Data Flow

1. **Initialization**: Program starts → Parse CLI options → Initialize MCP servers → Load conversation history
2. **Chat Loop**: User input → LLM processing → Tool function calls → Tool execution → Response streaming
3. **Persistence**: Conversation snapshots saved to `~/.bb-history/` with timestamp-based naming

### MCP Integration Pattern

The application uses the Model Context Protocol to connect with external tools:
- MCP servers configured in `~/.bb/mcp.json`
- Tools auto-discovered on startup via MCP handshake
- Function calls routed through MCP client abstraction
- Both streaming and non-streaming responses supported

### System Prompt Management

- Default system prompt created automatically in `~/.bb/system-prompt.txt`
- Project-specific context loaded from `CLAUDE.md` or `GEMINI.md` files
- Current working directory automatically appended to system context

### Token Usage and Cost Tracking

The TokenTracker component provides detailed analytics:
- Real-time token counting for input/output/cached tokens
- Cost calculation with model-specific pricing
- Context utilization monitoring to prevent limit overruns
- Session summaries and usage history persistence
- Support for prompt caching cost benefits (OpenAI)

### Configuration Files

- `~/.bb/system-prompt.txt`: User-customizable system prompt
- `~/.bb/mcp.json`: MCP server configuration
- `~/.bb-history/`: Conversation snapshots and HTTP request/response logs

### Streaming Response Handling

The application handles streaming responses with proper usage data extraction:
- Usage information parsed from final streaming chunk
- Support for cached token detection and cost savings display
- Reflection-based access to usage properties for different LLM providers

### Error Handling and Retry Logic

- Built-in rate limiting detection and retry with exponential backoff
- HTTP request/response logging for debugging
- Graceful handling of MCP server connection failures
- Context overflow prevention with automatic conversation truncation

## Development Notes

- Uses Microsoft.Extensions.AI for LLM abstraction
- ModelContextProtocol NuGet package for MCP client functionality
- Enhanced input handling with readline-style editing
- Cross-platform Unicode console support
- Built-in telemetry and metrics collection


