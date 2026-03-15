using Microsoft.Extensions.Logging;

namespace KF.Logging.Serilog;

/// <summary>
/// Factory for creating Serilog-backed event loggers.
/// </summary>
public interface ISerilogEventLoggerFactory
{
    /// <summary>
    /// Creates a Serilog event logger for the specified event.
    /// </summary>
    /// <typeparam name="TCategoryType">The category type for the logger.</typeparam>
    /// <param name="eventId">Numeric event identifier.</param>
    /// <param name="eventPath">Hierarchical event path.</param>
    /// <returns>A configured <see cref="IEventLogger"/> instance.</returns>
    IEventLogger Create<TCategoryType>(int eventId, string eventPath);

    /// <summary>
    /// Creates a Serilog event logger for the specified event and category.
    /// </summary>
    /// <param name="categoryName">The category name for the logger.</param>
    /// <param name="eventId">Numeric event identifier.</param>
    /// <param name="eventPath">Hierarchical event path.</param>
    /// <returns>A configured <see cref="IEventLogger"/> instance.</returns>
    IEventLogger Create(string categoryName, int eventId, string eventPath);
}
