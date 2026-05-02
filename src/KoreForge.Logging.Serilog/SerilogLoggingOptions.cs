using Microsoft.Extensions.Logging;

namespace KoreForge.Logging.Serilog;

/// <summary>
/// Configuration options for Serilog-based logging.
/// </summary>
public sealed class SerilogLoggingOptions
{
    /// <summary>
    /// Minimum log level. Default is Information.
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Whether to write to console. Default is true for development.
    /// </summary>
    public bool WriteToConsole { get; set; } = true;

    /// <summary>
    /// Whether to use compact JSON format for console output.
    /// </summary>
    public bool UseCompactJsonConsole { get; set; } = false;

    /// <summary>
    /// LogStash sink configuration. Null disables LogStash output.
    /// </summary>
    public LogStashOptions? LogStash { get; set; }

    /// <summary>
    /// Additional properties to include in all log events.
    /// </summary>
    public Dictionary<string, object> GlobalProperties { get; set; } = new();
}
