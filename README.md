# Blue Berry (bb)

A .NET 9 console application that connects LLMs to local tools via the Model Context Protocol (MCP). 
Think of it as a bridge between your favorite LLM and your development environment.

---

## 🚀 Just Want to Try It? (Non-Developers)

**Download and double-click to run:**

[![Download for Windows](https://img.shields.io/badge/Windows-Download%20%26%20Run-blue?style=for-the-badge&logo=windows)](https://github.com/bherrmann7/BlueBerry/releases/latest/download/blueberry-windows-x64.zip)
[![Download for macOS Intel](https://img.shields.io/badge/macOS-Intel-black?style=for-the-badge&logo=apple)](https://github.com/bherrmann7/BlueBerry/releases/latest/download/blueberry-macos-x64.zip)
[![Download for macOS Apple Silicon](https://img.shields.io/badge/macOS-Apple%20Silicon-black?style=for-the-badge&logo=apple)](https://github.com/bherrmann7/BlueBerry/releases/latest/download/blueberry-macos-arm64.zip)
[![Download for Linux](https://img.shields.io/badge/Linux-x64-orange?style=for-the-badge&logo=linux)](https://github.com/bherrmann7/BlueBerry/releases/latest/download/blueberry-linux-x64.zip)

### Quick Setup (5 minutes):
1. **Download** the file for your computer above
2. **Unzip** the downloaded file 
3. **Get a free API key** from [Cerebras](https://cloud.cerebras.ai?referral_code=y3wvtcmy) (they give you free tokens!)
4. **Run BlueBerry:**
   - **Windows**: Double-click `bb.exe`, or open Command Prompt and run:
     ```
     bb.exe --model qwen-3-coder-480b --endpoint https://api.cerebras.ai/v1 --key YOUR_API_KEY_HERE
     ```
   - **Mac/Linux**: Open Terminal in the folder and run:
     ```bash
     ./bb --model qwen-3-coder-480b --endpoint https://api.cerebras.ai/v1 --key YOUR_API_KEY_HERE
     ```

You should see a chat prompt where you can talk to the AI!

---

## 🛠️ Developer Path (Want to Hack on It?)

**Clone, build, and extend:**

```bash
git clone https://github.com/bherrmann7/BlueBerry.git
cd BlueBerry
dotnet build
dotnet run -- --model qwen-3-coder-480b --endpoint https://api.cerebras.ai/v1 --key $CEREBRAS_API_KEY
```

### Project Structure:
- `Program.cs` - Main entry point and REPL loop
- `ChatSession.cs` - Manages LLM conversations and function calling
- `McpClientManager.cs` - Handles MCP server connections
- `TokenTracker.cs` - Cost tracking and analytics
- `ConversationManager.cs` - Chat history and persistence

### Adding MCP Tools:
Create `~/.bb/mcp.json` (or `C:\Users\{you}\.bb\mcp.json` on Windows):

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

---

## What BlueBerry Does

- **Tool Integration**: Connects any OpenAI-compatible LLM to MCP servers (shell, file system, databases, etc.)
- **Cost Tracking**: Real-time token usage and spending analytics  
- **Context Monitoring**: Prevents hitting model context limits
- **Interactive REPL**: Chat with the LLM while it executes tools on your behalf

## Why Another Agent?

The AI coding assistant space is crowded, so why BlueBerry?

**Standards-Based**: Built on the Model Context Protocol (MCP) instead of custom integrations. Your tools work with other MCP clients, and other people's MCP servers work with BlueBerry. Less vendor lock-in, more interoperability.

**Cross-Platform .NET**: Most agents are Python-heavy and Linux-focused. BlueBerry runs natively on Windows, macOS, and Linux with proper Unicode support.

**Composable Architecture**: Rather than a monolithic framework, BlueBerry focuses on the core agent loop while delegating capabilities to specialized MCP servers. Want file operations? Add a file server. Need database access? Add a database server.

**Learning by Building**: Understanding how LLM function calling, token management, and tool integration work under the hood - not just consuming a black-box API.

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

Or view [all releases](https://github.com/bherrmann7/BlueBerry/releases) for previous versions.

---

Based on the [Microsoft MCP SDK]() • MIT License • *Have Fun! 🫐*