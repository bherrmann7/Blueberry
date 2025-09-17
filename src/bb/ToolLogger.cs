using System.Text.Json;
using Microsoft.Extensions.AI;

namespace BluelBerry;

public static class ToolLogger
{
    public static void LogToolCall(string toolName, string path, int byteCount)
    {
        Console.WriteLine($"🔧 Tool Call: {toolName} {path} bytes={byteCount}");
    }

    public static string SummarizeFunctionCall(FunctionCallContent call)
    {
        var args = call.Arguments ?? new Dictionary<string, object?>();

        // Helper to get path from common parameter names
        string? GetPath() =>
            args.TryGetValue("path", out var p1) ? p1?.ToString() :
            args.TryGetValue("file_path", out var p2) ? p2?.ToString() :
            args.TryGetValue("filepath", out var p3) ? p3?.ToString() : null;

        return call.Name switch
        {
            "read_file" => GetPath() is string path ? $"{call.Name} {path}" : call.Name,
            "write_file" => GetPath() is string path ? $"{call.Name} {path}" : call.Name,
            "list_directory" => GetPath() is string path ? $"{call.Name} {path}" : call.Name,
            "execute_shell_command" when args.TryGetValue("command", out var cmd) => $"{call.Name} \"{cmd}\"",
            "search_web" when args.TryGetValue("query", out var query) => $"{call.Name} \"{query}\"",
            "get_web_page_content" when args.TryGetValue("url", out var url) => $"{call.Name} {url}",
            "execute_query" when args.TryGetValue("query", out var query) => $"{call.Name} \"{query}\"",
            "execute_non_query" when args.TryGetValue("query", out var query) => $"{call.Name} \"{query}\"",
            "test_connection" => call.Name,
            "list_available_schemas" => call.Name,
            "create_directory" => GetPath() is string path ? $"{call.Name} {path}" : call.Name,
            "delete_file" => GetPath() is string path ? $"{call.Name} {path}" : call.Name,
            "move_file" when args.TryGetValue("source", out var src) && args.TryGetValue("destination", out var dest) =>
                $"{call.Name} {src} → {dest}",
            "copy_file" when args.TryGetValue("source", out var src) && args.TryGetValue("destination", out var dest) =>
                $"{call.Name} {src} → {dest}",
            _ => call.Name
        };
    }

    public static string SummarizeFunctionResult(FunctionResultContent result, string? functionName)
    {
        return functionName switch
        {
            "read_file" => "file read",
            "write_file" when result.Result is string filePath =>
                File.Exists(filePath) ? $"wrote {new FileInfo(filePath).Length} bytes" : "file written",
            "list_directory" => "directory listed",
            "execute_shell_command" => "command executed",
            "search_web" => "web search completed",
            "get_web_page_content" => "web page retrieved",
            "execute_query" => "query executed",
            "execute_non_query" => "non-query executed",
            "test_connection" => "connection tested",
            "list_available_schemas" => "schemas listed",
            "create_directory" => "directory created",
            "delete_file" => "file deleted",
            "move_file" => "file moved",
            "copy_file" => "file copied",
            _ => "completed"
        };
    }
}