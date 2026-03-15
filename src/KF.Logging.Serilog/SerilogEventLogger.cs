using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using ISerilogLogger = Serilog.ILogger;

namespace KF.Logging.Serilog;

/// <summary>
/// Serilog-backed implementation of <see cref="IEventLogger"/>.
/// Provides structured logging with EventId and EventPath enrichment.
/// </summary>
public sealed class SerilogEventLogger : IEventLogger
{
    private readonly ISerilogLogger _serilogLogger;
    private readonly Microsoft.Extensions.Logging.ILogger _msLogger;
    private readonly int _eventId;
    private readonly string _eventPath;

    /// <summary>
    /// Creates a new <see cref="SerilogEventLogger"/>.
    /// </summary>
    /// <param name="serilogLogger">The Serilog logger instance.</param>
    /// <param name="msLogger">The Microsoft.Extensions.Logging logger (for IsEnabled checks).</param>
    /// <param name="eventId">Numeric event identifier.</param>
    /// <param name="eventPath">Hierarchical event path (e.g., "MyApp.DB.Connection.Open").</param>
    public SerilogEventLogger(ISerilogLogger serilogLogger, Microsoft.Extensions.Logging.ILogger msLogger, int eventId, string eventPath)
    {
        _serilogLogger = serilogLogger ?? throw new ArgumentNullException(nameof(serilogLogger));
        _msLogger = msLogger ?? throw new ArgumentNullException(nameof(msLogger));
        _eventId = eventId;
        _eventPath = eventPath ?? throw new ArgumentNullException(nameof(eventPath));
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel level) => _msLogger.IsEnabled(level);

    private void LogInternal(LogLevel level, Exception? exception, string message, object?[]? args)
    {
        if (!IsEnabled(level))
            return;

        var serilogLevel = MapLogLevel(level);
        
        // Enrich with EventId and EventPath
        using (LogContext.PushProperty("EventId", _eventId))
        using (LogContext.PushProperty("EventIdName", _eventPath))
        using (LogContext.PushProperty("EventPath", _eventPath))
        {
            if (exception != null)
            {
                _serilogLogger.Write(serilogLevel, exception, message, args ?? Array.Empty<object?>());
            }
            else
            {
                _serilogLogger.Write(serilogLevel, message, args ?? Array.Empty<object?>());
            }
        }
    }

    private static global::Serilog.Events.LogEventLevel MapLogLevel(LogLevel level) => level switch
    {
        LogLevel.Trace => global::Serilog.Events.LogEventLevel.Verbose,
        LogLevel.Debug => global::Serilog.Events.LogEventLevel.Debug,
        LogLevel.Information => global::Serilog.Events.LogEventLevel.Information,
        LogLevel.Warning => global::Serilog.Events.LogEventLevel.Warning,
        LogLevel.Error => global::Serilog.Events.LogEventLevel.Error,
        LogLevel.Critical => global::Serilog.Events.LogEventLevel.Fatal,
        _ => global::Serilog.Events.LogEventLevel.Information
    };

    /// <inheritdoc />
    public void LogTrace(string message, params object?[] args) => LogInternal(LogLevel.Trace, null, message, args);
    /// <inheritdoc />
    public void LogTrace(Exception exception, string message, params object?[] args) => LogInternal(LogLevel.Trace, exception, message, args);

    /// <inheritdoc />
    public void LogDebug(string message, params object?[] args) => LogInternal(LogLevel.Debug, null, message, args);
    /// <inheritdoc />
    public void LogDebug(Exception exception, string message, params object?[] args) => LogInternal(LogLevel.Debug, exception, message, args);

    /// <inheritdoc />
    public void LogInformation(string message, params object?[] args) => LogInternal(LogLevel.Information, null, message, args);
    /// <inheritdoc />
    public void LogInformation(Exception exception, string message, params object?[] args) => LogInternal(LogLevel.Information, exception, message, args);

    /// <inheritdoc />
    public void LogWarning(string message, params object?[] args) => LogInternal(LogLevel.Warning, null, message, args);
    /// <inheritdoc />
    public void LogWarning(Exception exception, string message, params object?[] args) => LogInternal(LogLevel.Warning, exception, message, args);

    /// <inheritdoc />
    public void LogError(string message, params object?[] args) => LogInternal(LogLevel.Error, null, message, args);
    /// <inheritdoc />
    public void LogError(Exception exception, string message, params object?[] args) => LogInternal(LogLevel.Error, exception, message, args);

    /// <inheritdoc />
    public void LogCritical(string message, params object?[] args) => LogInternal(LogLevel.Critical, null, message, args);
    /// <inheritdoc />
    public void LogCritical(Exception exception, string message, params object?[] args) => LogInternal(LogLevel.Critical, exception, message, args);
}
