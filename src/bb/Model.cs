namespace BlueBerry;

/// <summary>
/// Represents a configured LLM model with its connection details.
/// Uses record type for value semantics while allowing mutation for key updates.
/// </summary>
public record Model
{
    public required string Name { get; set; }
    public required string ShortName { get; set; }
    public required string Endpoint { get; set; }
    public required string Key { get; set; }
}