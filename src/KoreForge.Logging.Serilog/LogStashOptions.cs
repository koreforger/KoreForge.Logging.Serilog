namespace KoreForge.Logging.Serilog;

/// <summary>
/// Configuration options for LogStash network sink.
/// </summary>
public sealed class LogStashOptions
{
    /// <summary>
    /// LogStash server hostname or IP address.
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// LogStash TCP input port. Default is 5000.
    /// </summary>
    public int Port { get; set; } = 5000;

    /// <summary>
    /// Whether to use TCP (true) or UDP (false). Default is TCP.
    /// </summary>
    public bool UseTcp { get; set; } = true;

    /// <summary>
    /// Application name to include in log entries.
    /// </summary>
    public string? ApplicationName { get; set; }

    /// <summary>
    /// Environment name (e.g., Development, Production).
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Whether to enable LogStash sink. Default is true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Buffer size for batching log events before sending.
    /// </summary>
    public int BufferSize { get; set; } = 1000;

    /// <summary>
    /// Gets the connection URI for the LogStash sink.
    /// </summary>
    internal string GetConnectionUri() => UseTcp 
        ? $"tcp://{Host}:{Port}" 
        : $"udp://{Host}:{Port}";
}
