namespace BlueBerry;

public record AppOptions(
    string model = "gpt-oss:20b",
    string endpoint = "http://127.0.0.1:11434/v1", // ollama
    string key = "not used with ollama",
    bool enableHttpLogging = false);