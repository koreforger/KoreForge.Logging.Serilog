using Microsoft.Extensions.Logging;
using Serilog;
using ISerilogLogger = Serilog.ILogger;

namespace KoreForge.Logging.Serilog;

/// <summary>
/// Default implementation of <see cref="ISerilogEventLoggerFactory"/>.
/// </summary>
public sealed class SerilogEventLoggerFactory : ISerilogEventLoggerFactory
{
    private readonly ISerilogLogger _serilogLogger;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Creates a new factory instance.
    /// </summary>
    /// <param name="serilogLogger">The root Serilog logger.</param>
    /// <param name="loggerFactory">Microsoft.Extensions.Logging factory for IsEnabled checks.</param>
    public SerilogEventLoggerFactory(ISerilogLogger serilogLogger, ILoggerFactory loggerFactory)
    {
        _serilogLogger = serilogLogger ?? throw new ArgumentNullException(nameof(serilogLogger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <inheritdoc />
    public IEventLogger Create<TCategoryType>(int eventId, string eventPath)
    {
        return Create(typeof(TCategoryType).FullName ?? typeof(TCategoryType).Name, eventId, eventPath);
    }

    /// <inheritdoc />
    public IEventLogger Create(string categoryName, int eventId, string eventPath)
    {
        var contextLogger = _serilogLogger.ForContext("SourceContext", categoryName);
        var msLogger = _loggerFactory.CreateLogger(categoryName);
        return new SerilogEventLogger(contextLogger, msLogger, eventId, eventPath);
    }
}
