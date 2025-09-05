# Blue Berry (bb)

Blue Berry is derived from an example in the Microsoft MCP Sdk.

Blue Berry is a .NET console application that demonstrates how to combine a large language model (LLM) with **tool-calling**
capabilities using the **Model Context Protocol (MCP)**. The program:

* Connects to any OpenAI compatible chat model (ie. Cerebras) 
* Connects to configured mcp servers
* Exposes the tools offered by the mcp servers to the LLM via function-calling.
* **📊 Tracks token usage and costs** in real-time with comprehensive analytics.
* **🔍 Monitors context utilization** to prevent hitting model limits.
* Enters an interactive REPL where you can ask the model to perform actions, and the results are displayed
  automatically.

## How to Create an AI Agent

Blue Berry started with sample code from https://github.com/modelcontextprotocol/csharp-sdk/blob/main/samples/ChatWithTools/Program.cs

The Microsoft MCP Library's examples show how to connect an LLM to MCP servers. This enables chat with tool support. With tools, an LLM can manipulate files and
execute programs.

Microsoft's MCP SDK example connects OpenAI Chat APIs to the Microsoft MCP Library. After adding shell/bash tools, you have everything you need to build an agent
that can help improve itself.

## Building the project

```bash
# Restore NuGet packages and build
dotnet build
```

## Running the application

### Starting the REPL

```bash
# Ensure the required environment variables are set
export OPENAI_API_KEY=your-openai-key   # or CEREBRAS_API_KEY

# Provide model and endpoint (the default is ollama)
dotnet run --model gpt-4o --endpoint https://api.openai.com/v1 --key your-key

# Sign up for free tokens from cerebras at (use the link for extra referal tokens) https://cloud.cerebras.ai?referral_code=y3wvtcmy
# to use Cerebras hosted Qwen3-480B (Coder)
dotnet run --model qwen-3-coder-480b --endpoint https://api.cerebras.ai/v1 --key $CEREBRAS_API_KEY

# Get help
dotnet run --help
```

## How it works (high-level)

1. **LLM client** – `OpenAIClient` (or Cerebras) provides the chat model.
2. **Token tracking** – `TokenTracker` monitors usage, costs, and context utilization in real-time.
3. **MCP clients** – `McpClientFactory.CreateAsync` starts the Bash and File servers via a *stdio* transport.
4. **Tool aggregation** – `ListToolsAsync` gathers the available functions from both servers.
5. **Function-calling** – The `IChatClient` is built with `UseFunctionInvocation()` so the LLM can request tool
   execution.
6. **REPL loop** – Reads user input, streams the LLM response, detects `FunctionCallContent` and
   `FunctionResultContent`, prints nicely formatted JSON, tracks costs, and feeds the updates back into the
   conversation.
7. **Analytics** – Session reports and conversation history are automatically saved for analysis.

## Features

- **Console Logging**: Simple console output for debugging and monitoring
- **Cost Monitoring**: Real-time spending tracking with model-specific pricing
- **Rate Limiting**: Built-in handling for API rate limits with retry logic

## License

This example is provided under the MIT License. Feel free to copy, modify, and redistribute.

---
*Have Fun! 🫐*
