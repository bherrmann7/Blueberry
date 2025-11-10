namespace BlueBerry;

/// <summary>
/// Exception thrown when the LLM provider's token quota has been exceeded.
/// This allows for graceful shutdown instead of Environment.Exit().
/// </summary>
public class QuotaExceededException : Exception
{
    public QuotaExceededException(string message) : base(message)
    {
    }

    public QuotaExceededException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
