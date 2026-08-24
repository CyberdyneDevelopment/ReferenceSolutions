using ReferenceNotifications.Console;
using Shouldly;
using Xunit;
using Fdw;
using Fdw.Services;
using Fdw.Services.Notifications;

namespace ReferenceNotifications.Console.Tests;

/// <summary>
/// Tests for ConsoleNotificationConfiguration properties and defaults.
/// </summary>
public sealed class ConsoleNotificationConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void DefaultLogLevelIsInformation()
    {
        // Arrange / Act
        var config = new ConsoleNotificationConfiguration();

        // Assert
        config.LogLevel.ShouldBe("Information");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void LogLevelPropertyIsSettable()
    {
        // Arrange
        var config = new ConsoleNotificationConfiguration();

        // Act
        config.LogLevel = "Debug";

        // Assert
        config.LogLevel.ShouldBe("Debug");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ServiceTypeIsConsole()
    {
        // Arrange / Act
        var config = new ConsoleNotificationConfiguration();

        // Assert
        config.ServiceOptionType.ShouldBe("Console");
    }
}
