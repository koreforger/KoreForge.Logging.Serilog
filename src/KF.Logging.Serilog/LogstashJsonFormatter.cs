using System.Text.Json;
using Serilog.Events;
using Serilog.Formatting;

namespace KF.Logging.Serilog;

/// <summary>
/// JSON formatter optimized for LogStash ingestion via ELK stack.
/// Outputs events in a format compatible with Logstash JSON codec.
/// </summary>
public sealed class LogstashJsonFormatter : ITextFormatter
{
    private readonly string? _applicationName;
    private readonly string? _environment;
    private static readonly string MachineName = Environment.MachineName;

    /// <summary>
    /// Creates a new LogStash JSON formatter.
    /// </summary>
    /// <param name="applicationName">Application name to include in events.</param>
    /// <param name="environment">Environment name to include in events.</param>
    public LogstashJsonFormatter(string? applicationName = null, string? environment = null)
    {
        _applicationName = applicationName;
        _environment = environment;
    }

    /// <inheritdoc />
    public void Format(LogEvent logEvent, TextWriter output)
    {
        var doc = new Dictionary<string, object?>
        {
            ["@timestamp"] = logEvent.Timestamp.UtcDateTime.ToString("O"),
            ["@version"] = "1",
            ["level"] = logEvent.Level.ToString(),
            ["message"] = logEvent.RenderMessage(),
            ["host"] = MachineName
        };

        if (_applicationName != null)
            doc["application"] = _applicationName;

        if (_environment != null)
            doc["environment"] = _environment;

        // Add EventId and EventPath if present
        if (logEvent.Properties.TryGetValue("EventId", out var eventId))
            doc["event_id"] = GetScalarValue(eventId);

        if (logEvent.Properties.TryGetValue("EventPath", out var eventPath))
            doc["event_path"] = GetScalarValue(eventPath);

        if (logEvent.Properties.TryGetValue("EventIdName", out var eventIdName))
            doc["event_name"] = GetScalarValue(eventIdName);

        // Add source context
        if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContext))
            doc["logger"] = GetScalarValue(sourceContext);

        // Add exception details
        if (logEvent.Exception != null)
        {
            doc["exception"] = new Dictionary<string, object?>
            {
                ["type"] = logEvent.Exception.GetType().FullName,
                ["message"] = logEvent.Exception.Message,
                ["stacktrace"] = logEvent.Exception.StackTrace
            };
        }

        // Add all other properties
        var fields = new Dictionary<string, object?>();
        foreach (var prop in logEvent.Properties)
        {
            if (prop.Key is "EventId" or "EventPath" or "EventIdName" or "SourceContext")
                continue;

            fields[prop.Key] = GetScalarValue(prop.Value);
        }

        if (fields.Count > 0)
            doc["fields"] = fields;

        output.WriteLine(JsonSerializer.Serialize(doc));
    }

    private static object? GetScalarValue(LogEventPropertyValue value)
    {
        return value switch
        {
            ScalarValue sv => sv.Value,
            SequenceValue seq => seq.Elements.Select(GetScalarValue).ToArray(),
            StructureValue str => str.Properties.ToDictionary(p => p.Name, p => GetScalarValue(p.Value)),
            DictionaryValue dict => dict.Elements.ToDictionary(
                kvp => GetScalarValue(kvp.Key)?.ToString() ?? "",
                kvp => GetScalarValue(kvp.Value)),
            _ => value.ToString()
        };
    }
}
