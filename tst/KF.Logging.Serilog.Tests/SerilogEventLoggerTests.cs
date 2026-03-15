using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.InMemory;
using Xunit;

namespace KF.Logging.Serilog.Tests;

/// <summary>
/// Tests for <see cref="SerilogEventLogger"/>.
/// </summary>
public sealed class SerilogEventLoggerTests
{
    [Fact]
    public void LogInformation_EmitsEventWithCorrectProperties()
    {
        // Arrange
        var inMemorySink = new InMemorySink();
        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .WriteTo.Sink(inMemorySink)
            .CreateLogger();

        using var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog(serilogLogger));
        var msLogger = loggerFactory.CreateLogger("TestCategory");
        
        var eventLogger = new SerilogEventLogger(serilogLogger, msLogger, 42, "MyApp.App.Start");

        // Act
        eventLogger.LogInformation("Application starting with value {Value}", 123);

        // Assert
        Assert.Single(inMemorySink.LogEvents);
        var logEvent = inMemorySink.LogEvents.First();
        
        Assert.Equal(LogEventLevel.Information, logEvent.Level);
        Assert.Contains("Application starting with value 123", logEvent.RenderMessage());
        
        // Verify enriched properties
        Assert.True(logEvent.Properties.ContainsKey("EventId"));
        Assert.Equal("42", logEvent.Properties["EventId"].ToString());
        
        Assert.True(logEvent.Properties.ContainsKey("EventPath"));
        Assert.Equal("\"MyApp.App.Start\"", logEvent.Properties["EventPath"].ToString());
    }

    [Fact]
    public void LogError_WithException_IncludesExceptionDetails()
    {
        // Arrange
        var inMemorySink = new InMemorySink();
        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .WriteTo.Sink(inMemorySink)
            .CreateLogger();

        using var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog(serilogLogger));
        var msLogger = loggerFactory.CreateLogger("TestCategory");
        
        var eventLogger = new SerilogEventLogger(serilogLogger, msLogger, 100, "MyApp.Error.Fatal");

        // Act
        var exception = new InvalidOperationException("Something went wrong");
        eventLogger.LogError(exception, "Failed to process request");

        // Assert
        Assert.Single(inMemorySink.LogEvents);
        var logEvent = inMemorySink.LogEvents.First();
        
        Assert.Equal(LogEventLevel.Error, logEvent.Level);
        Assert.NotNull(logEvent.Exception);
        Assert.IsType<InvalidOperationException>(logEvent.Exception);
    }

    [Fact]
    public void IsEnabled_RespectsMinimumLogLevel()
    {
        // Arrange
        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Warning()
            .CreateLogger();

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSerilog(serilogLogger);
            builder.SetMinimumLevel(LogLevel.Warning);
        });
        var msLogger = loggerFactory.CreateLogger("TestCategory");
        
        var eventLogger = new SerilogEventLogger(serilogLogger, msLogger, 1, "Test.Event");

        // Assert
        Assert.False(eventLogger.IsEnabled(LogLevel.Debug));
        Assert.False(eventLogger.IsEnabled(LogLevel.Information));
        Assert.True(eventLogger.IsEnabled(LogLevel.Warning));
        Assert.True(eventLogger.IsEnabled(LogLevel.Error));
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Critical)]
    public void AllLogLevels_MapCorrectlyToSerilog(LogLevel level)
    {
        // Arrange
        var inMemorySink = new InMemorySink();
        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .WriteTo.Sink(inMemorySink)
            .CreateLogger();

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSerilog(serilogLogger);
            builder.SetMinimumLevel(LogLevel.Trace);
        });
        var msLogger = loggerFactory.CreateLogger("TestCategory");
        
        var eventLogger = new SerilogEventLogger(serilogLogger, msLogger, 1, "Test.Event");

        // Act
        switch (level)
        {
            case LogLevel.Trace: eventLogger.LogTrace("test"); break;
            case LogLevel.Debug: eventLogger.LogDebug("test"); break;
            case LogLevel.Information: eventLogger.LogInformation("test"); break;
            case LogLevel.Warning: eventLogger.LogWarning("test"); break;
            case LogLevel.Error: eventLogger.LogError("test"); break;
            case LogLevel.Critical: eventLogger.LogCritical("test"); break;
        }

        // Assert
        Assert.Single(inMemorySink.LogEvents);
        var expectedLevel = level switch
        {
            LogLevel.Trace => LogEventLevel.Verbose,
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Critical => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };
        Assert.Equal(expectedLevel, inMemorySink.LogEvents.First().Level);
    }
}
