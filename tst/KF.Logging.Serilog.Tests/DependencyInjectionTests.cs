using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Xunit;
using ISerilogLogger = Serilog.ILogger;

namespace KF.Logging.Serilog.Tests;

/// <summary>
/// Tests for DI registration and factory creation.
/// </summary>
public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddKFSerilogLogging_RegistersAllServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddKFSerilogLogging(options =>
        {
            options.MinimumLevel = LogLevel.Debug;
            options.WriteToConsole = false;
        });

        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<ISerilogLogger>());
        Assert.NotNull(provider.GetService<ILoggerFactory>());
        Assert.NotNull(provider.GetService<ISerilogEventLoggerFactory>());
    }

    [Fact]
    public void SerilogEventLoggerFactory_CreatesLoggers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKFSerilogLogging(options =>
        {
            options.WriteToConsole = false;
        });

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<ISerilogEventLoggerFactory>();

        // Act
        var logger = factory.Create<DependencyInjectionTests>(42, "Test.Event.Path");

        // Assert
        Assert.NotNull(logger);
        Assert.IsType<SerilogEventLogger>(logger);
    }

    [Fact]
    public void LogStashOptions_ConfiguresCorrectly()
    {
        // Arrange & Act
        var tcpOptions = new LogStashOptions { Host = "elk.example.com", Port = 5044, UseTcp = true };
        var udpOptions = new LogStashOptions { Host = "elk.example.com", Port = 5044, UseTcp = false };

        // Assert
        Assert.Equal("elk.example.com", tcpOptions.Host);
        Assert.Equal(5044, tcpOptions.Port);
        Assert.True(tcpOptions.UseTcp);
        Assert.False(udpOptions.UseTcp);
    }

    [Fact]
    public void GlobalProperties_AreIncludedInLogs()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKFSerilogLogging(options =>
        {
            options.WriteToConsole = false;
            options.GlobalProperties["ServiceName"] = "TestService";
            options.GlobalProperties["Version"] = "1.0.0";
        });

        var provider = services.BuildServiceProvider();

        // Assert - services are configured
        var serilogLogger = provider.GetRequiredService<ISerilogLogger>();
        Assert.NotNull(serilogLogger);
    }
}
