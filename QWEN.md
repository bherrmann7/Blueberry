# Blue Berry (bb) - Project Context

## Project Overview

Blue Berry (bb) is a cross-platform .NET 9 console application that acts as a bridge between Large Language Models (LLMs) and local development tools. It leverages the Model Context Protocol (MCP) to integrate various tools (like shell access, file system operations) with any OpenAI-compatible LLM endpoint. The application provides an interactive REPL (Read-Eval-Print Loop) for chatting with the LLM, which can then execute tools on the user's behalf.

Key features include:
- Tool Integration via MCP servers
- Real-time token usage and cost tracking
- Context length monitoring to prevent exceeding model limits
- Persistent conversation history
- Enhanced input handling with multi-line editing and history

## Technology Stack

- **Language**: C# (.NET 9)
- **Core Libraries**:
  - `Microsoft.Extensions.AI`: Core AI abstractions.
  - `Microsoft.Extensions.AI.OpenAI`: OpenAI integration.
  - `Anthropic.SDK`: Anthropic API integration.
  - `ModelContextProtocol`: Official MCP SDK for .NET.
  - `ReadLine`: Enhanced console input handling.
- **Dependencies**: Various Microsoft.Extensions and OpenTelemetry packages.

## Project Structure

- **Main Entry Point**: `Program.cs` - Parses command-line arguments, initializes core components, and starts the chat session.
- **Chat Session**: `ChatSession.cs` - Manages the interactive REPL loop, handles user input, streams LLM responses, processes tool calls, and manages conversation state.
- **MCP Management**: `McpClientManager.cs` - Loads MCP server configurations from `~/.bb/mcp.json` and initializes connections to external tool servers.
- **MCP Configuration**: `McpConfig.cs` - Defines data structures for parsing the MCP server configuration JSON.
- **Options Parsing**: `AppOptions.cs` and `AppOptionsParser.cs` - Define and parse command-line arguments for model, endpoint, key, etc.
- **AI Client Factory**: `ChatClientFactory.cs` - Creates and configures the `IChatClient` instance based on provided options, including HTTP logging and rate limit handling.
- **Token Tracking**: `TokenTracker.cs` (content not provided but inferred from usage) - Tracks token usage and costs for the session.
- **Conversation Management**: `ConversationManager.cs` - Saves and loads conversation snapshots to/from `~/.bb-history/`.
- **Input Handling**: `EnhancedInputHandler.cs` - Provides a multi-line input editor with history and Emacs-style key bindings.
- **System Prompt**: `SystemPromptLoader.cs` - Loads the initial system prompt for the LLM.
- **Utility Helpers**:
  - `EnhancedTokenHelper.cs`: Assists with token estimation, formatting, and warnings.
  - Utility classes in `Program.cs` for cleaning strings and logging rate limits.

## Building and Running

1.  **Prerequisites**: .NET 9 SDK installed.
2.  **Build**: Run `dotnet build` in the project directory.
3.  **Run (Development)**: Run `dotnet run -- [arguments]` in the project directory.
    Example:
    ```bash
    dotnet run -- --model qwen-3-coder-480b --endpoint https://api.cerebras.ai/v1 --key YOUR_API_KEY_HERE
    ```
4.  **Run (Published)**: After publishing (`dotnet publish`), execute the generated binary (`bb` or `bb.exe`).

**Key Command-Line Arguments**:
- `--model`: The LLM model name (default: `gpt-oss:20b`).
- `--endpoint`: The LLM API endpoint URL (default: `http://127.0.0.1:11434/v1` for Ollama).
- `--key`: The API key for the LLM endpoint.

## Configuration

- **MCP Servers**: Configure external MCP tool servers by creating a JSON file at `~/.bb/mcp.json`.
  Example:
  ```json
  {
    "mcp_servers": [
      {
        "name": "Shell",
        "command": "/path/to/your/mcp-server",
        "arguments": []
      }
    ]
  }
  ```
- **History**: Conversation snapshots are saved to `~/.bb-history/`.
- **Input History**: Command-line input history is saved to `.bb-command-history` in the current directory.

## Development Conventions

- C# 9+ features (records, top-level statements in `Program.cs`, etc.) are used.
- Code is organized into a single project (`bb.csproj`) with clear separation of concerns via classes.
- Dependencies are managed via NuGet packages listed in `bb.csproj`.
- The application uses `Microsoft.Extensions` for logging and dependency injection patterns where applicable.