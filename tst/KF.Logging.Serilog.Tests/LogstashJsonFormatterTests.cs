using System.Text.Json;
using Serilog;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace KF.Logging.Serilog.Tests;

/// <summary>
/// Tests for <see cref="LogstashJsonFormatter"/>.
/// </summary>
public sealed class LogstashJsonFormatterTests
{
    [Fact]
    public void Format_ProducesValidJson()
    {
        // Arrange
        var formatter = new LogstashJsonFormatter("TestApp", "Development");
        var logEvent = CreateLogEvent(LogEventLevel.Information, "Test message");
        var writer = new StringWriter();

        // Act
        formatter.Format(logEvent, writer);
        var json = writer.ToString();

        // Assert - should be valid JSON
        var doc = JsonDocument.Parse(json);
        Assert.NotNull(doc);
    }

    [Fact]
    public void Format_IncludesRequiredFields()
    {
        // Arrange
        var formatter = new LogstashJsonFormatter("TestApp", "Production");
        var logEvent = CreateLogEvent(LogEventLevel.Warning, "Warning message");
        var writer = new StringWriter();

        // Act
        formatter.Format(logEvent, writer);
        var json = writer.ToString();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        Assert.True(root.TryGetProperty("@timestamp", out _));
        Assert.True(root.TryGetProperty("@version", out var version));
        Assert.Equal("1", version.GetString());
        Assert.True(root.TryGetProperty("level", out var level));
        Assert.Equal("Warning", level.GetString());
        Assert.True(root.TryGetProperty("message", out _));
        Assert.True(root.TryGetProperty("host", out _));
        Assert.True(root.TryGetProperty("application", out var app));
        Assert.Equal("TestApp", app.GetString());
        Assert.True(root.TryGetProperty("environment", out var env));
        Assert.Equal("Production", env.GetString());
    }

    [Fact]
    public void Format_IncludesEventIdAndPath()
    {
        // Arrange
        var formatter = new LogstashJsonFormatter();
        var properties = new List<LogEventProperty>
        {
            new("EventId", new ScalarValue(42)),
            new("EventPath", new ScalarValue("MyApp.DB.Query")),
            new("EventIdName", new ScalarValue("MyApp.DB.Query"))
        };
        var logEvent = new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            new MessageTemplate("Test", Array.Empty<MessageTemplateToken>()),
            properties);
        var writer = new StringWriter();

        // Act
        formatter.Format(logEvent, writer);
        var json = writer.ToString();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        Assert.True(root.TryGetProperty("event_id", out var eventId));
        Assert.Equal(42, eventId.GetInt32());
        Assert.True(root.TryGetProperty("event_path", out var eventPath));
        Assert.Equal("MyApp.DB.Query", eventPath.GetString());
    }

    [Fact]
    public void Format_IncludesException()
    {
        // Arrange
        var formatter = new LogstashJsonFormatter();
        var exception = new InvalidOperationException("Test exception");
        var logEvent = new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Error,
            exception,
            new MessageTemplate("Error occurred", Array.Empty<MessageTemplateToken>()),
            Array.Empty<LogEventProperty>());
        var writer = new StringWriter();

        // Act
        formatter.Format(logEvent, writer);
        var json = writer.ToString();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        Assert.True(root.TryGetProperty("exception", out var exc));
        Assert.True(exc.TryGetProperty("type", out var type));
        Assert.Equal("System.InvalidOperationException", type.GetString());
        Assert.True(exc.TryGetProperty("message", out var msg));
        Assert.Equal("Test exception", msg.GetString());
    }

    private static LogEvent CreateLogEvent(LogEventLevel level, string message)
    {
        return new LogEvent(
            DateTimeOffset.UtcNow,
            level,
            null,
            new MessageTemplate(message, Array.Empty<MessageTemplateToken>()),
            Array.Empty<LogEventProperty>());
    }
}
