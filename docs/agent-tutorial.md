# Understanding Agent Architecture in Blue Berry

## What is an AI Agent?

An **AI agent** is a program that autonomously achieves goals by combining:

1. **Language Models (LLMs)** - For reasoning and decision-making
2. **Tools** - For taking actions in the real world
3. **Memory** - For maintaining context across interactions

Think of it as the difference between a chatbot and a personal assistant:

- **Chatbot**: "I can only answer questions based on what I know"
- **Agent**: "I can look things up, execute commands, and complete tasks for you"

## The ReAct Pattern (Reason + Act)

Blue Berry implements the **ReAct pattern**, which alternates between:

- **Reasoning**: The LLM thinks about what to do
- **Acting**: The agent executes tools to gather information

### Example Flow

```
USER: "What's the weather like in Tokyo and how do I say 'hello' in Japanese?"

↓ REASON
LLM: "I need to get weather data and translate a word"

↓ ACT
Agent calls:
  - get_weather("Tokyo") → "72°F, sunny"
  - translate("hello", "Japanese") → "こんにちは (konnichiwa)"

↓ REASON
LLM: "Now I have both pieces of information"

↓ RESPOND
Agent: "It's 72°F and sunny in Tokyo! 'Hello' in Japanese is 'こんにちは' (konnichiwa)."
```

## Blue Berry Architecture

### Component Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                         ChatSession                          │
│                    (Main Agent Loop)                         │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  1. User Input → "Create a file called test.txt"      │ │
│  │  2. Add to conversation history                        │ │
│  │  3. Send to LLM with available tools                   │ │
│  │  4. LLM responds with function call                    │ │
│  │  5. Execute tool via MCP                               │ │
│  │  6. Add result to history, loop back to step 3        │ │
│  │  7. LLM responds with text → show to user             │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
        │                   │                    │
        ▼                   ▼                    ▼
┌──────────────┐    ┌──────────────┐    ┌─────────────────┐
│ Conversation │    │     MCP      │    │  Token Tracker  │
│   Manager    │    │  Client Mgr  │    │  (Usage/Cost)   │
│  (Memory)    │    │   (Tools)    │    │                 │
└──────────────┘    └──────────────┘    └─────────────────┘
```

### Key Components

#### 1. ChatSession (`ChatSession.cs`)

The heart of the agent. Implements the main ReAct loop:

```csharp
while (true) {
    prompt = ReadUserInput();

    // Send conversation history + available tools to LLM
    response = await LLM.GetResponse(conversationHistory, tools);

    // LLM returns either text or function calls
    // Function calls are automatically executed by middleware

    conversationHistory.Add(response);
    SaveConversation(conversationHistory);
}
```

**Location**: `src/bb/ChatSession.cs:101-179`
**Key Method**: `RunAsync()` - Main REPL loop

#### 2. MCP Client Manager (`McpClientManager.cs`)

Discovers and manages tools from MCP (Model Context Protocol) servers.

**What's MCP?** A protocol that lets agents discover and use tools from external servers. Think of it as a plugin system for agents.

```csharp
// MCP servers provide tools like:
//   - File system operations
//   - Web search
//   - Database queries
//   - Custom business logic

await mcpManager.InitializeAsync();  // Discover tools
var tools = mcpManager.Tools;         // Get available tools
// Tools are passed to the LLM so it knows what it can do
```

**Location**: `src/bb/McpClientManager.cs`
**Interface**: `IMcpClientManager`

#### 3. Conversation Manager (`ConversationManager.cs`)

Provides memory by saving and loading conversation history.

```csharp
// After each turn
conversationManager.SaveConversationSnapshot(messages);

// On startup or /resume command
var messages = conversationManager.LoadLatestConversation();
```

**Why is this important?** Without memory, the agent would forget everything between sessions. This persistence allows:
- Resuming conversations after crashes
- Debugging by reviewing conversation history
- Long-running tasks that span multiple sessions

**Location**: `src/bb/ConversationManager.cs`
**Interface**: `IConversationManager`

#### 4. Token Tracker (`TokenTracker.cs`)

Monitors costs and token usage - important because agents burn tokens fast!

```csharp
// After each LLM call
tokenTracker.TrackStreamingUsage(response, modelName, contextLength);

// Show summary
tokenTracker.PrintSessionSummary();
// → "Total cost: $0.0234, Tokens: 2,340 in + 567 out"
```

**Location**: `src/bb/TokenTracker.cs`
**Interface**: `ITokenTracker`

## The Token Burning Problem

⚠️ **Blue Berry is intentionally inefficient for educational purposes!**

### The Problem

Every LLM request sends the **entire conversation history**:

```
Turn 1: Send 100 tokens → Response 50 tokens
Turn 2: Send 150 tokens (100 + 50 old) → Response 50 tokens
Turn 3: Send 200 tokens (150 + 50 old) → Response 50 tokens
...
Turn 10: Send 550 tokens → Response 50 tokens

Total tokens SENT: 2,750 tokens
Total tokens of ACTUAL content: 600 tokens
Efficiency: 22% 🔥🔥🔥
```

For a 100-turn conversation, you might send **500,000 tokens** for only **10,000 tokens** of actual content!

### Why Does Blue Berry Do This?

1. **Simplicity**: The code is easy to understand
2. **Educational**: Understanding the naive approach helps you appreciate optimizations
3. **Correctness**: The LLM always has full context

### How Production Agents Solve This

#### 1. Conversation Summarization

```
Turns 1-20: "User configured settings and ran tests"  (condensed)
Turns 21-30: [full conversation]                       (recent context)
Current turn: "How do I deploy?"                       (active)
```

**Benefit**: Keeps important info while reducing token count by 70-90%

#### 2. Sliding Window

```
Keep only:
  - System prompt
  - Last 10 messages
  - Current prompt

Drop everything else
```

**Benefit**: Constant token usage regardless of conversation length
**Tradeoff**: Agent forgets older context

#### 3. RAG (Retrieval-Augmented Generation)

```
Store conversation in vector database
→ User asks "What did we decide about authentication?"
→ Search vector DB for relevant messages
→ Send only those messages + current prompt
```

**Benefit**: Agent can access long-term memory efficiently
**Used by**: ChatGPT, Claude Projects, most production agents

#### 4. Semantic Caching

Some LLM providers (OpenAI, Anthropic) cache repeated context:

```
First request: Send 10,000 tokens → Full cost
Second request: Same 10,000 tokens + 100 new → 90% discount on cached tokens
```

**Blue Berry shows this!** Check `TokenTracker.cs:165-177` for cached token handling.

## Code Walkthrough: A Full Agent Turn

Let's trace what happens when you type: `"Create a file called hello.txt with content 'Hello, World!'"`

### Step 1: User Input (`ChatSession.cs:127-129`)

```csharp
var prompt = EnhancedInputHandler.ReadInput();
_lastPrompt = prompt;
messages.Add(new ChatMessage(ChatRole.User, prompt));
```

Conversation history now looks like:
```
[System]: You are an expert software engineer...
[User]: Create a file called hello.txt with content 'Hello, World!'
```

### Step 2: Send to LLM (`ChatSession.cs:234-256`)

```csharp
// THE AGENT CORE
await foreach (var update in _chatClient.GetStreamingResponseAsync(
    messages,  // Full conversation history
    new ChatOptions { Tools = [.. _mcpManager.Tools] }  // Available tools
))
```

What the LLM receives:
```json
{
  "messages": [
    {"role": "system", "content": "You are an expert..."},
    {"role": "user", "content": "Create a file called hello.txt..."}
  ],
  "tools": [
    {"name": "write_file", "parameters": {"path": "string", "content": "string"}},
    {"name": "read_file", "parameters": {"path": "string"}},
    // ... more tools
  ]
}
```

### Step 3: LLM Decides to Use a Tool

The LLM responds:
```json
{
  "function_call": {
    "name": "write_file",
    "arguments": {
      "path": "hello.txt",
      "content": "Hello, World!"
    }
  }
}
```

### Step 4: Function Invocation Middleware Executes Tool

The `.UseFunctionInvocation()` middleware (configured in `ChatClientFactory.cs:43-46`) automatically:

1. Detects the function call
2. Calls the MCP server's `write_file` tool
3. Gets the result: `"File written successfully"`
4. Adds to conversation history:
   ```
   [Assistant]: <function_call: write_file(...)>
   [Tool]: File written successfully
   ```
5. Loops back to the LLM with updated history

### Step 5: LLM Responds with Text

Now the conversation history is:
```
[System]: You are an expert...
[User]: Create a file called hello.txt with content 'Hello, World!'
[Assistant]: <function_call: write_file(path="hello.txt", content="Hello, World!")>
[Tool]: File written successfully
```

The LLM thinks: "Great! The file was created. I can respond now."

LLM Response:
```
"I've created hello.txt with the content 'Hello, World!'"
```

### Step 6: Save State (`ChatSession.cs:136`)

```csharp
_conversationManager.SaveConversationSnapshot(messages);
```

The conversation is saved to `~/.bb-history/bb-{timestamp}.json` for later resumption.

### Step 7: Track Usage (`ChatSession.cs:131-134`)

```csharp
_tokenTracker.TrackStreamingUsage(updates, _options.model, finalContextLength, _maxTokens);
```

Console output:
```
🔢 Request Token Usage: prompt=234, completion=67, cached=0, total=301 | 💰 Cost: $0.000752
```

## Extending Blue Berry

### Adding a New Tool (via MCP)

1. **Find an MCP server** - Check [modelcontextprotocol.io/servers](https://modelcontextprotocol.io/servers)
2. **Add to config** (`~/.bb/mcp.json`):
   ```json
   {
     "mcp_servers": [
       {
         "name": "filesystem",
         "command": "npx",
         "arguments": ["-y", "@modelcontextprotocol/server-filesystem", "/home/user"]
       }
     ]
   }
   ```
3. **Restart Blue Berry** - Tools are discovered automatically

### Creating Your Own MCP Server

```typescript
// example-tool-server.ts
import { Server } from "@modelcontextprotocol/sdk/server/index.js";

const server = new Server({
  name: "example-tools",
  version: "1.0.0",
});

server.tool("get_current_time", "Gets the current time", async () => {
  return { content: [{ type: "text", text: new Date().toISOString() }] };
});

await server.connect();
```

## Learning Path

### Beginner: Understanding the Basics
1. Read the class-level documentation in `ChatSession.cs:10-100`
2. Trace through one agent turn (see "Code Walkthrough" above)
3. Run Blue Berry and watch the tool calls happen in real-time
4. Check the saved conversations in `~/.bb-history/`

### Intermediate: Seeing the Inefficiency
1. Have a 20-turn conversation
2. Run `summary` command to see token usage
3. Look at the conversation JSON files - see how big they get
4. Read the "Token Burning Problem" section above

### Advanced: Building Better Agents
1. Implement conversation summarization (compress every 10 turns)
2. Add a sliding window (keep only last N messages)
3. Integrate a vector database for semantic search
4. Implement prompt caching detection (Blue Berry shows this already!)

## Next Steps

- **MCP Integration**: Read `docs/mcp-integration.md` (if it exists)
- **Token Management**: Read `docs/token-management.md` (if it exists)
- **Architecture Deep Dive**: Read the interface documentation in `src/bb/Interfaces/`

## Resources

- [ReAct Paper](https://arxiv.org/abs/2210.03629) - Original research on Reasoning + Acting
- [Model Context Protocol](https://modelcontextprotocol.io) - Official MCP documentation
- [Microsoft.Extensions.AI](https://devblogs.microsoft.com/dotnet/introducing-microsoft-extensions-ai-preview/) - The LLM abstraction Blue Berry uses
- [Agent Patterns](https://www.anthropic.com/research/building-effective-agents) - Anthropic's guide to agent architectures

---

**Remember**: Blue Berry is a learning tool, not a production framework. It burns tokens like a dumpster fire 🔥 so you can understand *why* production agents need sophisticated memory management!
