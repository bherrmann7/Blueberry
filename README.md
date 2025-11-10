# Blueberry (bb)

<img src="blueberry.png" alt="Blue Berry" width="50%"/>

Another AI Agent, essentially the "Hello World" of agentic computeing.    This repos is a .NET 9 console application that connects LLMs to local tools via the Model Context Protocol (MCP).
Think of it as a bridge between your favorite LLM and your development environment.

<!-- Trigger release workflow -->

---

## 🚀 Just Want to run it?

**Download and double-click to run:**

[![Download for Windows](https://img.shields.io/badge/Windows-Download%20%26%20Run-blue?style=for-the-badge&logo=windows)](https://github.com/bherrmann7/Blueberry/releases/latest/download/bb-windows-x64.zip)
[![Download for macOS Intel](https://img.shields.io/badge/macOS-Intel-black?style=for-the-badge&logo=apple)](https://github.com/bherrmann7/Blueberry/releases/latest/download/bb-macos-x64.zip)
[![Download for macOS Apple Silicon](https://img.shields.io/badge/macOS-Apple%20Silicon-black?style=for-the-badge&logo=apple)](https://github.com/bherrmann7/Blueberry/releases/latest/download/bb-macos-arm64.zip)
[![Download for Linux](https://img.shields.io/badge/Linux-x64-orange?style=for-the-badge&logo=linux)](https://github.com/bherrmann7/Blueberry/releases/latest/download/bb-linux-x64.zip)

### Quick Setup:
1. **Download** the file for your computer above
2. **Unzip** the downloaded file
3. **Set up LLM** You can use Ollama, or from [Cerebras](https://cloud.cerebras.ai?referral_code=y3wvtcmy) (they have a free tier)
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

### Usage Options:

Once installed, you can run BlueBerry in several ways:

1. **Using model short names** (requires `~/.bb/models.json` configuration):
   ```bash
   bb <model-short-name>
   ```
   This uses the short name to automatically set the model, endpoint, and key from your `~/.bb/models.json` configuration file.

2. **Display configured MCP servers**:
   ```bash
   bb mcp
   ```
   Shows the currently configured MCP servers from `~/.bb/mcp.json` and exits.

---

## 🛠️ Developer Path

**Clone, build, and extend:**

```bash
git clone https://github.com/bherrmann7/Blueberry.git
cd Blueberry
dotnet build
dotnet run --project src/bb -- --model qwen-3-coder-480b --endpoint https://api.cerebras.ai/v1 --key $CEREBRAS_API_KEY
```

### Project Structure:
- `src/bb/` - Main application source code
  - `Program.cs` - Main entry point and REPL loop
  - `ChatSession.cs` - Manages LLM conversations and function calling
  - `McpClientManager.cs` - Handles MCP server connections
  - `TokenTracker.cs` - Cost tracking and analytics
  - `ConversationManager.cs` - Chat history and persistence
- `tests/bb.Tests/` - Unit tests

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

If you add the https://github.com/bherrmann7/shell-mcp-server mcp server, then Blueberry can immediately be used for improving itself.

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

## How Blue Berry Works (Agent Architecture)

### What is an AI Agent?

An AI agent goes beyond a simple chatbot - it can **reason** about problems and **act** using tools to solve them autonomously.

**Chatbot**: "I can answer questions based on what I know"
**Agent**: "I can look things up, run commands, and complete tasks for you"

### The ReAct Loop (Reason + Act)

Blue Berry implements the **ReAct pattern** - the LLM alternates between thinking and acting:

```
USER: "What files are in my home directory?"
  ↓
REASON: LLM thinks "I need to list directory contents"
  ↓
ACT: Blue Berry calls list_directory("/home/user") via MCP
  ↓
REASON: LLM thinks "Now I have the list, I can answer"
  ↓
RESPOND: "Your home directory contains: file1.txt, file2.txt..."
```

### The Agent Loop in Blue Berry

```
┌──────────────────────────────────────────────────────────┐
│  1. User types: "Create a file hello.txt"               │
│  2. Add to conversation history                          │
│  3. Send history + available tools to LLM                │
│  4. LLM responds with function call: write_file(...)     │
│  5. Execute tool via MCP server                          │
│  6. Add result to history, go back to step 3             │
│  7. LLM responds with text → show to user                │
│  8. Save conversation, repeat from step 1                │
└──────────────────────────────────────────────────────────┘
```

**See it in action**: Watch the console output - you'll see `🔧 Tool Call` and `🔄 Tool Result` as the agent works!

### The Token Burning Problem

⚠️ **Blue Berry is intentionally simple but inefficient!**

Every request sends the **entire conversation history** to the LLM:

```
Turn 1:  Send 100 tokens  → Response 50 tokens
Turn 2:  Send 150 tokens  → Response 50 tokens  (includes turn 1)
Turn 3:  Send 200 tokens  → Response 50 tokens  (includes turns 1-2)
...
Turn 10: Send 550 tokens  → Response 50 tokens  (includes turns 1-9)

Total sent: 2,750 tokens
Actual content: 600 tokens
Efficiency: 22% 🔥
```

This makes the agent expensive and slow as conversations grow!

**Why does Blue Berry do this?** For learning! Understanding this naive approach helps you appreciate why production agents need:
- **Conversation Summarization**: Compress old turns into summaries
- **Sliding Windows**: Keep only recent messages
- **RAG (Retrieval-Augmented Generation)**: Vector databases for semantic memory
- **Semantic Caching**: LLM providers cache repeated context

**Want to learn more?** Read [`docs/agent-tutorial.md`](docs/agent-tutorial.md) for a deep dive into agent architecture!

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

Sign up at [Cerebras](https://cloud.cerebras.ai?referral_code=y3wvtcmy) for free tokens to get started (this referral link is supposed to get you an extra 200k / day tokens)

Or view [all releases](https://github.com/bherrmann7/Blueberry/releases) for previous versions.

---

Based on the [Microsoft MCP SDK]() • MIT License • *Have Fun! 🤠*

Specifically from https://github.com/modelcontextprotocol/csharp-sdk/blob/main/samples/ChatWithTools/Program.cs
