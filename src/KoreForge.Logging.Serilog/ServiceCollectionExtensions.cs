using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Network;
using ISerilogLogger = Serilog.ILogger;

namespace KoreForge.Logging.Serilog;

/// <summary>
/// Extension methods for configuring Serilog-based logging.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Serilog-based logging with optional LogStash network sink.
    /// This replaces the default Microsoft.Extensions.Logging provider.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKFSerilogLogging(
        this IServiceCollection services,
        Action<SerilogLoggingOptions>? configure = null)
    {
        var options = new SerilogLoggingOptions();
        configure?.Invoke(options);

        services.Configure<SerilogLoggingOptions>(opt =>
        {
            opt.MinimumLevel = options.MinimumLevel;
            opt.WriteToConsole = options.WriteToConsole;
            opt.UseCompactJsonConsole = options.UseCompactJsonConsole;
            opt.LogStash = options.LogStash;
            foreach (var prop in options.GlobalProperties)
            {
                opt.GlobalProperties[prop.Key] = prop.Value;
            }
        });

        // Build Serilog logger
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Is(MapLogLevel(options.MinimumLevel))
            .Enrich.FromLogContext();

        // Add global properties
        foreach (var prop in options.GlobalProperties)
        {
            loggerConfig = loggerConfig.Enrich.WithProperty(prop.Key, prop.Value);
        }

        // Console sink
        if (options.WriteToConsole)
        {
            if (options.UseCompactJsonConsole)
            {
                loggerConfig = loggerConfig.WriteTo.Console(new CompactJsonFormatter());
            }
            else
            {
                loggerConfig = loggerConfig.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{EventPath}] {Message:lj}{NewLine}{Exception}");
            }
        }

        // LogStash network sink
        if (options.LogStash?.Enabled == true)
        {
            var logStash = options.LogStash;
            if (logStash.UseTcp)
            {
                loggerConfig = loggerConfig.WriteTo.TCPSink(
                    logStash.Host,
                    logStash.Port,
                    new LogstashJsonFormatter(logStash.ApplicationName, logStash.Environment));
            }
            else
            {
                loggerConfig = loggerConfig.WriteTo.UDPSink(
                    logStash.Host,
                    logStash.Port,
                    new LogstashJsonFormatter(logStash.ApplicationName, logStash.Environment));
            }
        }

        var serilogLogger = loggerConfig.CreateLogger();

        // Register as singleton
        services.AddSingleton<ISerilogLogger>(serilogLogger);
        
        // Add Serilog to Microsoft.Extensions.Logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(serilogLogger, dispose: true);
        });

        // Register factory
        services.AddSingleton<ISerilogEventLoggerFactory, SerilogEventLoggerFactory>();

        return services;
    }

    /// <summary>
    /// Adds Serilog-based logging configured from IConfiguration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationSection">Configuration section name. Default is "Serilog".</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKFSerilogLoggingFromConfiguration(
        this IServiceCollection services,
        string configurationSection = "Logging:Serilog")
    {
        // This variant would bind from IConfiguration
        // For now, use the programmatic approach
        return services.AddKFSerilogLogging();
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
}
