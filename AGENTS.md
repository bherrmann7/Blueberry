# Repository Guidelines

This repository contains Blue Berry (bb), a .NET 9 console app that connects LLMs to local tools via the Model Context Protocol (MCP).

## Project Structure & Modules
- Root-level C# files (no `src/` folder): `Program.cs`, `ChatSession.cs`, `McpClientManager.cs`, `TokenTracker.cs`, `ConversationManager.cs`, etc.
- Build metadata: `bb.csproj`. CI: `.github/`.
- Runtime data (not committed): `~/.bb-history/` (sessions/logs), `~/.bb/mcp.json` (MCP servers), `~/.bb/system-prompt.txt`.

## Build, Test, and Development
- Build: `dotnet build`
- Run (OpenAI): `dotnet run -- --model gpt-4o --endpoint https://api.openai.com/v1 --key $OPENAI_API_KEY`
- Run (Cerebras): `dotnet run -- --model qwen-3-coder-480b --endpoint https://api.cerebras.ai/v1 --key $CEREBRAS_API_KEY`
- Package (self-contained):
  - `dotnet publish -c Release -r win-x64 --self-contained`
  - `dotnet publish -c Release -r osx-x64 --self-contained`
  - `dotnet publish -c Release -r linux-x64 --self-contained`
- Format: `dotnet format`

## Coding Style & Naming
- C# latest; 4-space indentation; file-scoped namespaces.
- Types/methods/properties: PascalCase. Locals/params: camelCase. Private fields: `_camelCase`.
- Enable nullable reference types; prefer `var` when obvious; explicit access modifiers.
- One public type per file; keep `Program.cs` thin—put logic in classes (e.g., `ChatSession`).

## Testing Guidelines
- Framework: xUnit (planned). Create `tests/BlueBerry.Tests/` with files like `ChatSessionTests.cs`.
- Run tests: `dotnet test`.
- Naming: `{TypeName}Tests.MethodUnderTest_ShouldExpectedBehavior`.
- Avoid real network calls—abstract via interfaces (e.g., mock LLM/MCP clients).

## Commit & Pull Request Guidelines
- Commits: imperative, concise, and scoped (e.g., "Add token usage summary"). Include the “why” when not obvious.
- PRs: clear description, what/why, linked issues (`Fixes #123`), CLI examples, and relevant logs/screenshots. Note any breaking flags/API changes and update `README.md`/`CLAUDE.md` accordingly.

## Security & Configuration
- Never commit API keys or tokens. Prefer env vars: `$OPENAI_API_KEY`, `$CEREBRAS_API_KEY`.
- Keep secrets out of logs; review `~/.bb-history/` before sharing.
- MCP servers configured in `~/.bb/mcp.json`:
  ```json
  { "mcp_servers": [{ "name": "Shell", "command": "/path/to/server", "arguments": [] }] }
  ```

## Agent-Specific Notes
- Follow this guide’s style and structure. Make minimal, focused changes.
- Do not rename files unnecessarily. Update docs when behavior or flags change.
